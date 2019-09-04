using System;
using System.Text;
using System.Runtime.InteropServices;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb device
    /// </summary>
    public class UsbDevice : Device
    {
        private string _comPort = string.Empty;

        private enum DeviceControlFlags
        {
            Enable,
            Disable
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbDevice"/> class.
        /// </summary>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        public UsbDevice(UsbSpec.UsbDeviceDescriptor deviceDescriptor, uint adapterNumber)
            : base(deviceDescriptor, adapterNumber, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbDevice"/> class.
        /// </summary>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbDevice(UsbSpec.UsbDeviceDescriptor deviceDescriptor, uint adapterNumber, string devicePath)
            : base(deviceDescriptor, adapterNumber, devicePath)
        {
        }

        public string ComPort {
            get
            {
                if(string.IsNullOrEmpty(_comPort))
                {
                    GetComPort();
                }

                return _comPort;
            }
        }

        public bool OpenDevice()
        {
            throw new NotImplementedException();

            /*bool erg = false;
            // SETLASTERROR !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            erg = UsbApi.CloseHandle(m_FileHandle);
            m_FileHandle = UsbApi.CreateFile(m_DevicePath, UsbApi.GENERIC_READ, UsbApi.FILE_SHARE_READ, IntPtr.Zero, UsbApi.OPEN_EXISTING, 0, IntPtr.Zero);
            byte[] buffer = new byte[20];
            uint bytesread = 0;
            int error = 0;
            System.Threading.NativeOverlapped overlapped = new System.Threading.NativeOverlapped();
            IntPtr ptr = Marshal.AllocCoTaskMem(1000);
            UsbApi.SetLastError(0);
            int nBytesReturned = 0;
            erg = UsbApi.DeviceIoControl(m_FileHandle, UsbApi.USBUSER_GET_CONTROLLER_DRIVER_KEY, IntPtr.Zero, 0, ptr, 1000, out nBytesReturned, IntPtr.Zero);
            if (erg)
                Console.WriteLine("true");
            else
                Console.WriteLine("false");
            return true;*/
        }

        /// <summary>
        /// Disables the specified device.
        /// </summary>
        /// <param name="vendorid">The vendorid.</param>
        /// <param name="productid">The productid.</param>
        /// <returns></returns>
        public bool Disable(ushort vendorid, ushort productid)
        {
            return SetDevice(DeviceControlFlags.Disable, vendorid, productid);
        }

        /// <summary>
        /// Enables the specified device.
        /// </summary>
        /// <param name="vendorid">The vendorid.</param>
        /// <param name="productid">The productid.</param>
        /// <returns></returns>
        public bool Enable(ushort vendorid, ushort productid)
        {
            return SetDevice(DeviceControlFlags.Enable, vendorid, productid);
        }

        private bool SetDevice(DeviceControlFlags deviceControlFlag, ushort vendorid, ushort productid)
        {
            Guid myGuid = Guid.Empty;
            var deviceInfoData = new UsbApi.SpDevinfoData1
            {
                CbSize = 28,
                DevInst = 0,
                ClassGuid = Guid.Empty,
                Reserved = 0
            };
            UInt32 i = 0;
            StringBuilder deviceName = new StringBuilder()
            {
                Capacity = UsbApi.MaxBufferSize
            };

            //The SetupDiGetClassDevs function returns a handle to a device information set that contains requested device information elements for a local machine.
            IntPtr theDevInfo = UsbApi.SetupDiGetClassDevs(ref myGuid, 0, 0, UsbApi.DigcfAllclasses | UsbApi.DigcfPresent | UsbApi.DigcfProfile);
            for (; UsbApi.SetupDiEnumDeviceInfo(theDevInfo, i, deviceInfoData);)
            {

                if (UsbApi.SetupDiGetDeviceRegistryProperty(theDevInfo, deviceInfoData, (uint)UsbApi.Spdrp.SpdrpHardwareid, 0, deviceName, UsbApi.MaxBufferSize, IntPtr.Zero))
                {
                    if (deviceName.ToString().Contains(@"USB\Vid_") && deviceName.ToString().Contains(vendorid.ToString("x")))
                    {
                        StateChange(
                            deviceControlFlag == DeviceControlFlags.Disable ? UsbApi.DicsDisable : UsbApi.DicsEnable,
                            (int)i, theDevInfo);

                        UsbApi.SetupDiDestroyDeviceInfoList(theDevInfo);
                        break;
                    }
                }
                i++;
            }

            return true;
        }

        private void StateChange(int newState, int selectedItem, IntPtr hDevInfo)
        {
            var propChangeParams = new UsbApi.SpPropchangeParams();
            propChangeParams.Init();
            var deviceInfoData = new UsbApi.SpDevinfoData();
            propChangeParams.ClassInstallHeader.CbSize = Marshal.SizeOf(propChangeParams.ClassInstallHeader);
            deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);

            if (!UsbApi.SetupDiEnumDeviceInfo(hDevInfo, selectedItem, ref deviceInfoData))
                return;

            propChangeParams.ClassInstallHeader.InstallFunction = UsbApi.DifPropertychange;
            propChangeParams.Scope = UsbApi.DicsFlagGlobal;
            propChangeParams.StateChange = newState;

            if (!UsbApi.SetupDiSetClassInstallParams(hDevInfo, ref deviceInfoData, ref propChangeParams.ClassInstallHeader, Marshal.SizeOf(propChangeParams)))
                return;

            UsbApi.SetupDiCallClassInstaller(UsbApi.DifPropertychange, hDevInfo, ref deviceInfoData);
        }

        private void GetComPort()
        {
            IntPtr regValue = IntPtr.Zero;
            IntPtr regKey = IntPtr.Zero;
            IntPtr handle = IntPtr.Zero;
            IntPtr ptr = IntPtr.Zero;

            try
            {
                handle =
                    UsbApi.SetupDiGetClassDevs(0, UsbApi.RegstrKeyUsb, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfAllclasses);

                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                    var success = true;

                    for (var i = 0; success; i++)
                    {
                        var deviceInfoData = new UsbApi.SpDevinfoData();
                        deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);

                        success = UsbApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInfoData);

                        if (success)
                        {
                            var requiredSize = -1;
                            var regType = UsbApi.RegSz;
                            var driverKey = string.Empty;

                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData,
                                (int)UsbApi.Spdrp.SpdrpDriver,
                                ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            {
                                driverKey = Marshal.PtrToStringAuto(ptr);
                            }

                            if(DriverKey == driverKey)
                            {
                                regKey = UsbApi.SetupDiOpenDevRegKey(handle, ref deviceInfoData, UsbApi.DicsFlag.Global, 0, UsbApi.DiReg.Dev, (uint) WinNtApi.KeyRead);

                                int size = 0;
                                Advapi32.RegValueKind kind = Advapi32.RegValueKind.None;

                                // Get the size of buffer we will need
                                uint retVal = Advapi32.RegQueryValueEx(regKey, "PortName", 0, ref kind, IntPtr.Zero, ref size);
                                if (size == 0)
                                {
                                    break;
                                }

                                regValue = Marshal.AllocHGlobal(size);

                                if(Advapi32.RegQueryValueEx(regKey, "PortName", 0, ref kind, regValue, ref size) == 0)
                                {
                                    if(kind == Advapi32.RegValueKind.Sz)
                                    {
                                        _comPort = Marshal.PtrToStringAnsi(regValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if(regValue != IntPtr.Zero)
                    Marshal.FreeHGlobal(regValue);

                if (regKey != IntPtr.Zero)
                    Marshal.FreeHGlobal(regKey);

                if (handle != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }
        }
    }
}
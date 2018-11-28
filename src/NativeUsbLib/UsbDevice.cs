#region references

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

#endregion

namespace NativeUsbLib
{
    /// <summary>
    /// Usb device
    /// </summary>
    public class UsbDevice : Device
    {
        #region enum DeviceControlFlags

        private enum DeviceControlFlags
        {
            Enable,
            Disable
        }

        #endregion

        #region constructor/destructor

        #region constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UsbDevice"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        public UsbDevice(Device parent, UsbApi.USB_DEVICE_DESCRIPTOR deviceDescriptor, int adapterNumber)
            : base(parent, deviceDescriptor, adapterNumber, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbDevice"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbDevice(Device parent, UsbApi.USB_DEVICE_DESCRIPTOR deviceDescriptor, int adapterNumber, string devicePath)
            : base(parent, deviceDescriptor, adapterNumber, devicePath)
        {
        }

        #endregion

        #endregion

        #region methodes

        #region OpenDevice

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

        #endregion

        #region methode Disable

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

        #endregion

        #region methode Enable

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

        #endregion

        #region methode SetDevice

        private bool SetDevice(DeviceControlFlags deviceControlFlag, ushort vendorid, ushort productid)
        {
            Guid myGuid = System.Guid.Empty;
            var deviceInfoData = new UsbApi.SP_DEVINFO_DATA1
            {
                cbSize = 28,
                DevInst = 0,
                ClassGuid = System.Guid.Empty,
                Reserved = 0
            };
            UInt32 i = 0;
            StringBuilder deviceName = new StringBuilder()
            {
                Capacity = UsbApi.MAX_BUFFER_SIZE
            };

            //The SetupDiGetClassDevs function returns a handle to a device information set that contains requested device information elements for a local machine.
            IntPtr theDevInfo = UsbApi.SetupDiGetClassDevs(ref myGuid, 0, 0, UsbApi.DIGCF_ALLCLASSES | UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_PROFILE);
            for (; UsbApi.SetupDiEnumDeviceInfo(theDevInfo, i, deviceInfoData); )
            {

                if (UsbApi.SetupDiGetDeviceRegistryProperty(theDevInfo, deviceInfoData, (uint)UsbApi.SPDRP.SPDRP_HARDWAREID, 0, deviceName, UsbApi.MAX_BUFFER_SIZE, IntPtr.Zero))
                {
                    if (deviceName.ToString().Contains(@"USB\Vid_") && deviceName.ToString().Contains(vendorid.ToString("x")))
                    {

                        if (deviceControlFlag == DeviceControlFlags.Disable)
                            StateChange(UsbApi.DICS_DISABLE, (int)i, theDevInfo);
                        else
                            StateChange(UsbApi.DICS_ENABLE, (int)i, theDevInfo);

                        UsbApi.SetupDiDestroyDeviceInfoList(theDevInfo);
                        break;
                    }
                }
                i++;
            }

            return true;
        }

        #endregion

        #region methode GetDeviceInstanceId

        private string GetDeviceInstanceId(IntPtr deviceInfoSet, UsbApi.SP_DEVINFO_DATA deviceInfoData)
        {
            StringBuilder strId = new StringBuilder(0);
            Int32 iRequiredSize;
            Int32 iSize = 0;
            bool success = UsbApi.SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, strId, iSize, out iRequiredSize);
            strId = new StringBuilder(iRequiredSize);
            iSize = iRequiredSize;
            success = UsbApi.SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, strId, iSize, out iRequiredSize);

            if (success)
                return strId.ToString();

            return String.Empty;
        }

        #endregion

        #region methode StateChange

        private bool StateChange(int newState, int selectedItem, IntPtr hDevInfo)
        {
            var propChangeParams = new UsbApi.SP_PROPCHANGE_PARAMS();
            propChangeParams.Init();
            var deviceInfoData = new UsbApi.SP_DEVINFO_DATA();
            propChangeParams.ClassInstallHeader.cbSize = Marshal.SizeOf(propChangeParams.ClassInstallHeader);
            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

            if (!UsbApi.SetupDiEnumDeviceInfo(hDevInfo, selectedItem, ref deviceInfoData))
                return false;

            propChangeParams.ClassInstallHeader.InstallFunction = UsbApi.DIF_PROPERTYCHANGE;
            propChangeParams.Scope = UsbApi.DICS_FLAG_GLOBAL;
            propChangeParams.StateChange = newState;

            if (!UsbApi.SetupDiSetClassInstallParams(hDevInfo, ref deviceInfoData, ref propChangeParams.ClassInstallHeader, Marshal.SizeOf(propChangeParams)))
                return false;

            if (!UsbApi.SetupDiCallClassInstaller(UsbApi.DIF_PROPERTYCHANGE, hDevInfo, ref deviceInfoData))
                return false;

            return true;
        }

        #endregion

        #endregion
    }
}
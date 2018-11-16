#region references

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

#endregion

namespace NativeUsbLib
{
    /// <summary>
    /// Usb controller
    /// </summary>
    public class UsbController : Device
    {
        #region fields

        private Guid m_InterfaceClassGuid = Guid.Empty;
        private Guid m_Guid = new Guid(UsbApi.GUID_DEVINTERFACE_HUBCONTROLLER);

        #endregion

        #region constructor/destructor

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbController"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="index">The index.</param>
        public UsbController(Device parent, int index)
            : base(parent, null, index, null)
        {
            IntPtr ptr = IntPtr.Zero;
            IntPtr handle = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(UsbApi.MAX_BUFFER_SIZE);
                bool success = true;
                handle = UsbApi.SetupDiGetClassDevs(ref m_Guid, 0, IntPtr.Zero, UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_DEVICEINTERFACE);

                // Create a device interface data structure
                UsbApi.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new UsbApi.SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                // Start the enumeration.
                success = UsbApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref m_Guid, index, ref deviceInterfaceData);
                if (success)
                {
                    m_InterfaceClassGuid = deviceInterfaceData.InterfaceClassGuid;

                    // Build a DevInfo data structure.
                    UsbApi.SP_DEVINFO_DATA deviceInfoData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                    // Build a device interface detail data structure.
                    UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData = new UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA();
                    deviceInterfaceDetailData.cbSize = UIntPtr.Size == 8 ? 8 : (int)(4 + (uint)Marshal.SystemDefaultCharSize);

                    // Now we can get some more detailed informations.
                    int nRequiredSize = 0;
                    int nBytes = UsbApi.MAX_BUFFER_SIZE;
                    if (UsbApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                    {
                        this.m_DevicePath = deviceInterfaceDetailData.DevicePath;

                        // Get the device description and driver key name.
                        int requiredSize = 0;
                        int regType = UsbApi.REG_SZ;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SPDRP_DEVICEDESC, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            this.m_DeviceDescription = Marshal.PtrToStringAuto(ptr);
                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SPDRP_DRIVER, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            this.m_DriverKey = Marshal.PtrToStringAuto(ptr);
                    }

                    try
                    {
                        this.devices.Add(new UsbHub(this, null, this.m_DevicePath));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        //this.m_UsbHub = null;
                        throw new Exception(ex.Message);
                    }
                }
                else
                    throw new Exception("No usb controller found!");
            }
            finally
            {
                if(ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }
        }

        #endregion

        #region destructor

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="UsbController"/> is reclaimed by garbage collection.
        /// </summary>
        ~UsbController()
        {
        }

        #endregion

        #endregion

        #region proberties

        #region Hubs

        /// <summary>
        /// Gets the hubs.
        /// </summary>
        /// <value>The hubs.</value>
        public System.Collections.ObjectModel.ReadOnlyCollection<UsbHub> Hubs
        {
            get
            {
                UsbHub[] _hub = new UsbHub[devices.Count];
                devices.CopyTo(_hub);
                return new System.Collections.ObjectModel.ReadOnlyCollection<UsbHub>(_hub);
            }
        }

        #endregion

        #endregion
    }
}
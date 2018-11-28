#region references

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

#endregion

namespace NativeUsbLib
{
    /// <summary>
    /// Usb controller
    /// </summary>
    public class UsbController : Device
    {
        #region fields

        private readonly Guid m_Guid = new Guid(UsbApi.GUID_DEVINTERFACE_HUBCONTROLLER);
        private Guid m_InterfaceClassGuid = Guid.Empty;

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
                handle = UsbApi.SetupDiGetClassDevs(ref m_Guid, 0, IntPtr.Zero, UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_DEVICEINTERFACE);

                // Create a device interface data structure
                UsbApi.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new UsbApi.SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                // Start the enumeration.
                Boolean success = UsbApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref m_Guid, index, ref deviceInterfaceData);
                if (success)
                {
                    m_InterfaceClassGuid = deviceInterfaceData.InterfaceClassGuid;

                    // Build a DevInfo data structure.
                    UsbApi.SP_DEVINFO_DATA deviceInfoData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                    // Build a device interface detail data structure.
                    UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData =
                        new UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA
                        {
                            cbSize = UIntPtr.Size == 8 ? 8 : (int) (4 + (uint) Marshal.SystemDefaultCharSize)
                        };

                    // Now we can get some more detailed informations.
                    int nRequiredSize = 0;
                    int nBytes = UsbApi.MAX_BUFFER_SIZE;
                    if (UsbApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                    {
                        DevicePath = deviceInterfaceDetailData.DevicePath;

                        // Get the device description and driver key name.
                        int requiredSize = 0;
                        int regType = UsbApi.REG_SZ;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SPDRP_DEVICEDESC, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            DeviceDescription = Marshal.PtrToStringAuto(ptr);
                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SPDRP_DRIVER, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            DriverKey = Marshal.PtrToStringAuto(ptr);
                    }

                    try
                    {
                        Devices.Add(new UsbHub(this, null, DevicePath));
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

        #endregion

        #region proberties

        #region Hubs

        /// <summary>
        /// Gets the hubs.
        /// </summary>
        /// <value>The hubs.</value>
        public ReadOnlyCollection<UsbHub> Hubs
        {
            get
            {
                UsbHub[] hubs = new UsbHub[Devices.Count];
                Devices.CopyTo(hubs);
                return new ReadOnlyCollection<UsbHub>(hubs);
            }
        }

        #endregion

        #endregion
    }
}
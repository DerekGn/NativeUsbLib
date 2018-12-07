using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using NativeUsbLib.Exceptions;
using System.Diagnostics;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb controller
    /// </summary>
    public class UsbController : Device
    {
        #region fields

        private readonly Guid m_Guid = new Guid(UsbApi.GuidDevinterfaceHubcontroller);

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
                ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                handle = UsbApi.SetupDiGetClassDevs(ref m_Guid, 0, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);

                // Create a device interface data structure
                UsbApi.SpDeviceInterfaceData deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                // Start the enumeration.
                Boolean success = UsbApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref m_Guid, index, ref deviceInterfaceData);
                if (success)
                {
                    // Build a DevInfo data structure.
                    UsbApi.SpDevinfoData deviceInfoData = new UsbApi.SpDevinfoData();
                    deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);

                    // Build a device interface detail data structure.
                    UsbApi.SpDeviceInterfaceDetailData deviceInterfaceDetailData =
                        new UsbApi.SpDeviceInterfaceDetailData
                        {
                            CbSize = UIntPtr.Size == 8 ? 8 : (int) (4 + (uint) Marshal.SystemDefaultCharSize)
                        };

                    // Now we can get some more detailed informations.
                    int nRequiredSize = 0;
                    int nBytes = UsbApi.MaxBufferSize;
                    if (UsbApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                    {
                        DevicePath = deviceInterfaceDetailData.DevicePath;

                        // Get the device description and driver key name.
                        int requiredSize = 0;
                        int regType = UsbApi.RegSz;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SpdrpDevicedesc, ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            DeviceDescription = Marshal.PtrToStringAuto(ptr);
                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData, UsbApi.SpdrpDriver, ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            DriverKey = Marshal.PtrToStringAuto(ptr);
                    }

                    try
                    {
                        Devices.Add(new UsbHub(this, null, DevicePath));
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Unhandled exception occurred: {0}", ex.ToString());

                        throw new UsbControllerException("Unhandled exception occurred", ex);
                    }
                }
                else
                    throw new UsbControllerException("No usb controller found!");
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
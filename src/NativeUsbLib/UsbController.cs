using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using NativeUsbLib.Exceptions;
using System.Diagnostics;
using System.Globalization;
using NativeUsbLib.WinApis;
using NativeUsbLib.Diagnostics;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb controller
    /// </summary>
    public class UsbController : Device
    {
        private Guid _guid = new Guid(UsbApi.GuidDevinterfaceHubcontroller);

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbController"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="index">The index.</param>
        public UsbController(Device parent, uint index)
            : base(null, index, null)
        {
            PowerInfo = new List<UsbUser.UsbPowerInfo>();

            Initalise(index);
        }

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

        public uint VendorId { get; private set; }

        public uint DeviceId { get; private set; }

        public uint SubSysId { get; private set; }

        public uint Revision { get; private set; }

        public IList<UsbUser.UsbPowerInfo> PowerInfo { get; }

        public UsbUser.UsbControllerInfo0 ControllerInfo { get; private set; }

        private void ParseDevicePath(string devicePath)
        {
            var parts = devicePath.Split('#');

            if (parts.Length == 4)
            {
                var details = parts[1].Split('&');

                if (details.Length == 4)
                {
                    VendorId = uint.Parse(details[0].Substring(4), NumberStyles.AllowHexSpecifier);
                    DeviceId = uint.Parse(details[1].Substring(4), NumberStyles.AllowHexSpecifier);
                    SubSysId = uint.Parse(details[2].Substring(7), NumberStyles.AllowHexSpecifier);
                    Revision = uint.Parse(details[3].Substring(4), NumberStyles.AllowHexSpecifier);
                }
            }
        }

        private void Initalise(uint index)
        {
            IntPtr ptr = IntPtr.Zero;
            IntPtr deviceInfoHandle = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                deviceInfoHandle = UsbApi.SetupDiGetClassDevs(ref _guid, 0, IntPtr.Zero,
                    UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);

                // Create a device interface data structure
                UsbApi.SpDeviceInterfaceData deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                // Start the enumeration.
                Boolean success =
                    UsbApi.SetupDiEnumDeviceInterfaces(deviceInfoHandle, IntPtr.Zero, ref _guid, (int) index,
                        ref deviceInterfaceData);
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

                    if (UsbApi.SetupDiGetDeviceInterfaceDetail(deviceInfoHandle, ref deviceInterfaceData,
                        ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                    {
                        DevicePath = deviceInterfaceDetailData.DevicePath;

                        ParseDevicePath(DevicePath);

                        // Get the device description and driver key name.
                        int requiredSize = 0;
                        int regType = UsbApi.RegSz;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(deviceInfoHandle, ref deviceInfoData,
                            (int) UsbApi.Spdrp.SpdrpDevicedesc,
                            ref regType,
                            ptr,
                            UsbApi.MaxBufferSize, ref requiredSize))
                            DeviceDescription = Marshal.PtrToStringAuto(ptr);

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(
                            deviceInfoHandle,
                            ref deviceInfoData,
                            (int) UsbApi.Spdrp.SpdrpDriver,
                            ref regType,
                            ptr,
                            UsbApi.MaxBufferSize,
                            ref requiredSize))
                        {
                            DriverKey = Marshal.PtrToStringAuto(ptr);
                        }
                    }

                    IntPtr hostControllerHandle = IntPtr.Zero;

                    try
                    {
                        hostControllerHandle = KernelApi.CreateFile(deviceInterfaceDetailData.DevicePath,
                            UsbApi.GenericWrite,
                            UsbApi.FileShareWrite, IntPtr.Zero,
                            UsbApi.OpenExisting, 0, IntPtr.Zero);

                        if (deviceInfoHandle.ToInt64() != UsbApi.InvalidHandleValue)
                        {
                            GetHostControllerPowerMap(hostControllerHandle);

                            GetHostControllerInfo(hostControllerHandle);
                        }
                    }
                    finally
                    {
                        if (hostControllerHandle != IntPtr.Zero ||
                            deviceInfoHandle.ToInt64() != UsbApi.InvalidHandleValue)
                            KernelApi.CloseHandle(hostControllerHandle);
                    }

                    try
                    {
                        Devices.Add(new UsbHub(null, DevicePath));
                    }
                    catch (Exception ex)
                    {
                        CoreTraceSource.Source.TraceEvent(TraceEventType.Error, CoreTraceSource.UsbControllerSourceId,
                            "Unhandled exception occurred: {0}", ex);

                        throw new UsbControllerException("Unhandled exception occurred", ex);
                    }
                }
                else
                    throw new UsbControllerException("No usb controller found!");
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (deviceInfoHandle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(deviceInfoHandle);
            }
        }

        private void GetHostControllerInfo(IntPtr handle)
        {
            IntPtr ptrUsbControllerInfo0 = IntPtr.Zero;

            try
            {
                // set the header and request sizes
                UsbUser.UsbuserControllerInfo0 usbControllerInfo0 =
                    new UsbUser.UsbuserControllerInfo0 {Header = {UsbUserRequest = UsbUser.UsbuserGetControllerInfo0}};

                usbControllerInfo0.Header.RequestBufferLength = (uint) Marshal.SizeOf(usbControllerInfo0);

                //
                // Query for the USB_CONTROLLER_INFO_0 structure
                //
                int bytesRequested = Marshal.SizeOf(usbControllerInfo0);
                
                ptrUsbControllerInfo0 = Marshal.AllocHGlobal(bytesRequested);
                Marshal.StructureToPtr(usbControllerInfo0, ptrUsbControllerInfo0, true);

                if (KernelApi.DeviceIoControl(handle,
                    UsbUser.IoctlUsbUserRequest,
                    ptrUsbControllerInfo0,
                    bytesRequested,
                    ptrUsbControllerInfo0,
                    bytesRequested,
                    out _,
                    IntPtr.Zero))
                {
                    CoreTraceSource.Source.TraceEvent(TraceEventType.Error, CoreTraceSource.UsbControllerSourceId,
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbUser.IoctlUsbUserRequest)}] Result: [{KernelApi.GetLastError():X}]");
                }
                else
                {
                    usbControllerInfo0 = (UsbUser.UsbuserControllerInfo0) Marshal.PtrToStructure(ptrUsbControllerInfo0,
                        typeof(UsbUser.UsbuserControllerInfo0));
                    ControllerInfo = usbControllerInfo0.Info0;
                }
            }
            finally
            {
                if (ptrUsbControllerInfo0 != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrUsbControllerInfo0);
                }
            }
        }

        private void GetHostControllerPowerMap(IntPtr handle)
        {
            UsbUser.WdmusbPowerState powerState = UsbUser.WdmusbPowerState.WdmUsbPowerSystemWorking;

            for (; powerState <= UsbUser.WdmusbPowerState.WdmUsbPowerSystemShutdown; powerState++)
            {
                UsbUser.UsbuserPowerInfoRequest powerInfoRequest = new UsbUser.UsbuserPowerInfoRequest
                {
                    Header =
                    {
                        UsbUserRequest = UsbUser.UsbuserGetPowerStateMap,
                    },
                    PowerInformation = {SystemState = powerState}
                };

                powerInfoRequest.Header.RequestBufferLength = (uint) Marshal.SizeOf(powerInfoRequest);

                IntPtr ptrPowerInfoRequest = IntPtr.Zero;

                try
                {
                    //
                    // Now query USBHUB for the USB_POWER_INFO structure for this hub.
                    // For Selective Suspend support
                    //
                    int nBytes = Marshal.SizeOf(powerInfoRequest);
                    ptrPowerInfoRequest = Marshal.AllocHGlobal(nBytes);

                    Marshal.StructureToPtr(powerInfoRequest, ptrPowerInfoRequest, true);

                    var success = KernelApi.DeviceIoControl(handle,
                        UsbUser.IoctlUsbUserRequest,
                        ptrPowerInfoRequest,
                        nBytes,
                        ptrPowerInfoRequest,
                        nBytes,
                        out _,
                        IntPtr.Zero);

                    if (!success)
                    {
                        CoreTraceSource.Source.TraceEvent(TraceEventType.Error, CoreTraceSource.UsbControllerSourceId,
                            $"[{nameof(KernelApi.DeviceIoControl)}] Returned Error Code: [{KernelApi.GetLastError():X}]");
                    }
                    else
                    {
                        powerInfoRequest = (UsbUser.UsbuserPowerInfoRequest)Marshal.PtrToStructure(ptrPowerInfoRequest,
                            typeof(UsbUser.UsbuserPowerInfoRequest));
                        PowerInfo.Add(powerInfoRequest.PowerInformation);
                    }
                }
                finally
                {
                    if(ptrPowerInfoRequest != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptrPowerInfoRequest);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using NativeUsbLib.Exceptions;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using NativeUsbLib.WinApis;

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
        public UsbController(Device parent, int index)
            : base(parent, null, index, null)
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

        public IList<UsbUser.UsbPowerInfo> PowerInfo { get; private set; }

        public bool BusDeviceFunctionValid { get; private set; }

        public uint BusNumber { get; private set; }

        public ushort BusDevice { get; private set; }

        public ushort BusFunction { get; private set; }

        //public PUSB_CONTROLLER_INFO_0 ControllerInfo { get; private set; }

        //public USB_DEVICE_PNP_STRINGS UsbDeviceProperties { get; private set; }

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

                //
                // Now query USBHUB for the USB_POWER_INFO structure for this hub.
                // For Selective Suspend support
                //
                int nBytesReturned = 0;
                int nBytes = Marshal.SizeOf(powerInfoRequest);
                IntPtr ptrPowerInfoRequest = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(powerInfoRequest, ptrPowerInfoRequest, true);

                var success = KernelApi.DeviceIoControl(handle,
                    UsbUser.IoctlUsbUserRequest,
                    ptrPowerInfoRequest,
                    nBytes,
                    ptrPowerInfoRequest,
                    nBytes,
                    out nBytesReturned,
                    IntPtr.Zero);

                if (!success)
                {
                    Trace.WriteLine(
                        $"[{nameof(KernelApi.DeviceIoControl)}] Returned Error Code: [{KernelApi.GetLastError():X}]");
                }
                else
                {
                    powerInfoRequest = (UsbUser.UsbuserPowerInfoRequest)Marshal.PtrToStructure(ptrPowerInfoRequest, typeof(UsbUser.UsbuserPowerInfoRequest));
                    PowerInfo.Add(powerInfoRequest.PowerInformation);
                }

                Marshal.FreeHGlobal(ptrPowerInfoRequest);
            }
        }

        private void Initalise(int index)
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
                    UsbApi.SetupDiEnumDeviceInterfaces(deviceInfoHandle, IntPtr.Zero, ref _guid, index,
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
                            UsbApi.SpdrpDevicedesc,
                            ref regType, 
                            ptr,
                            UsbApi.MaxBufferSize, ref requiredSize))
                            DeviceDescription = Marshal.PtrToStringAuto(ptr);

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(
                            deviceInfoHandle,
                            ref deviceInfoData,
                            UsbApi.SpdrpDriver,
                            ref regType,
                            ptr,
                            UsbApi.MaxBufferSize,
                            ref requiredSize))
                        {
                            DriverKey = Marshal.PtrToStringAuto(ptr);
                        }

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(
                            deviceInfoHandle,
                            ref deviceInfoData,
                            UsbApi.SpdrpDriver,
                            ref regType,
                            ptr,
                            Marshal.SizeOf(BusNumber),
                            ref requiredSize))
                        {
                            BusNumber = Marshal.ReadByte(ptr);
                        }

                        //if (success)
                        //{
                        //    success = SetupDiGetDeviceRegistryProperty(deviceInfo,
                        //        deviceInfoData,
                        //        SPDRP_ADDRESS,
                        //        NULL,
                        //        (PBYTE) & deviceAndFunction,
                        //        sizeof(deviceAndFunction),
                        //        NULL);
                        //}

                        //if (success)
                        //{
                        //    hcInfo->BusDevice = deviceAndFunction >> 16;
                        //    hcInfo->BusFunction = deviceAndFunction & 0xffff;
                        //    hcInfo->BusDeviceFunctionValid = TRUE;
                        //}
                    }

                    IntPtr hostControllerHandle = IntPtr.Zero;

                    try
                    {
                        hostControllerHandle = KernelApi.CreateFile(deviceInterfaceDetailData.DevicePath,
                            UsbApi.GenericWrite,
                            UsbApi.FileShareWrite, IntPtr.Zero,
                            UsbApi.OpenExisting, 0, IntPtr.Zero);

                        if (deviceInfoHandle.ToInt64() != UsbApi.InvalidHandleValue)
                            GetHostControllerPowerMap(hostControllerHandle);
                    }
                    finally
                    {
                        if (hostControllerHandle != IntPtr.Zero ||
                            deviceInfoHandle.ToInt64() != UsbApi.InvalidHandleValue)
                            KernelApi.CloseHandle(hostControllerHandle);
                    }

                    try
                    {
                        Devices.Add(new UsbHub(this, null, DevicePath));
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Unhandled exception occurred: {0}", ex);

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
    }
}
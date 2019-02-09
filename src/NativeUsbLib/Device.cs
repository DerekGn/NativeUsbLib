using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    ///     Abstract base class of all usb devices
    /// </summary>
    public abstract class Device : IDisposable
    {
        private UsbIoControl.UsbNodeConnectionInformationEx _mNodeConnectionInfo;
        protected List<Device> Devices;

        /// <summary>
        ///     Gets the child devices
        /// </summary>
        public ReadOnlyCollection<Device> ChildDevices => new ReadOnlyCollection<Device>(Devices);

        /// <summary>
        ///     Gets or sets the node connection info.
        /// </summary>
        /// <value>The node connection info.</value>
        public UsbIoControl.UsbNodeConnectionInformationEx NodeConnectionInfo
        {
            get => _mNodeConnectionInfo;
            set
            {
                _mNodeConnectionInfo = value;

                if (NodeConnectionInfo.ConnectionIndex != 0)
                    AdapterNumber = NodeConnectionInfo.ConnectionIndex;

                var status = NodeConnectionInfo.ConnectionStatus;
                Status = status.ToString();
                Speed = NodeConnectionInfo.Speed;
                IsConnected = NodeConnectionInfo.ConnectionStatus == UsbIoControl.UsbConnectionStatus.DeviceConnected;
                IsHub = Convert.ToBoolean(NodeConnectionInfo.DeviceIsHub);
            }
        }

        /// <summary>
        ///     Gets or sets the device descriptor.
        /// </summary>
        /// <value>The device descriptor.</value>
        public UsbSpec.UsbDeviceDescriptor DeviceDescriptor { get; set; }


        /// <summary>
        ///     Gets the configuration descriptor.
        /// </summary>
        /// <value>The configuration descriptor.</value>
        public List<UsbSpec.UsbConfigurationDescriptor> ConfigurationDescriptors { get; } =
            new List<UsbSpec.UsbConfigurationDescriptor>();

        /// <summary>
        ///     Gets the interface descriptor.
        /// </summary>
        /// <value>The interface descriptor.</value>
        public List<UsbSpec.UsbInterfaceDescriptor> InterfaceDescriptors { get; } =
            new List<UsbSpec.UsbInterfaceDescriptor>();

        /// <summary>
        ///     Gets the endpoint descriptor.
        /// </summary>
        /// <value>The endpoint descriptor.</value>
        public List<UsbSpec.UsbEndpointDescriptor> EndpointDescriptors { get; } =
            new List<UsbSpec.UsbEndpointDescriptor>();

        /// <summary>
        ///     Gets the hdi descriptor.
        /// </summary>
        /// <value>The hdi descriptor.</value>
        public List<UsbDesc.HidDescriptor> HidDescriptors { get; } = new List<UsbDesc.HidDescriptor>();

        /// <summary>
        ///     Gets the device path.
        /// </summary>
        /// <value>The device path.</value>
        public string DevicePath { get; protected set; }

        /// <summary>
        ///     Gets the underlying USB device path for a HID device.
        /// </summary>
        /// <value>The underlying USB device path if a HID device, otherwise an empty string.</value>
        public string UsbDevicePath { get; protected set; } = string.Empty;

        /// <summary>
        ///     Gets the driver key.
        /// </summary>
        /// <value>The driver key.</value>
        public string DriverKey { get; protected set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        ///     Gets the device description.
        /// </summary>
        /// <value>The device description.</value>
        public string DeviceDescription { get; protected set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is hub.
        /// </summary>
        /// <value><c>true</c> if this instance is hub; otherwise, <c>false</c>.</value>
        public bool IsHub { get; private set; }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the speed.
        /// </summary>
        /// <value>The speed.</value>
        public UsbSpec.UsbDeviceSpeed Speed { get; private set; }

        /// <summary>
        ///     Gets or sets the adapter number.
        /// </summary>
        /// <value>The adapter number.</value>
        public uint AdapterNumber { get; private set; }

        /// <summary>
        ///     Gets the manufacturer.
        /// </summary>
        /// <value>The manufacturer.</value>
        public string Manufacturer { get; } = string.Empty;

        /// <summary>
        ///     Gets the instance id.
        /// </summary>
        /// <value>The instance id.</value>
        public string InstanceId { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the serial number.
        /// </summary>
        /// <value>The serial number.</value>
        public string SerialNumber { get; } = string.Empty;

        /// <summary>
        ///     Gets the product.
        /// </summary>
        /// <value>The product.</value>
        public string Product { get; } = string.Empty;

        public UsbIoControl.UsbPortConnectorProperties UsbPortConnectorProperties { get; internal set; }

        public UsbIoControl.UsbNodeConnectionInformationExV2 NodeConnectionInfoV2 { get; internal set; }


        /// <summary>
        ///     Initializes a new instance of the <see cref="Device" /> class.
        /// </summary>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        /// <param name="devicePath">The device path.</param>
        protected Device(UsbSpec.UsbDeviceDescriptor deviceDescriptor, uint adapterNumber,
            string devicePath)
        {
            AdapterNumber = adapterNumber;
            DeviceDescriptor = deviceDescriptor;
            DevicePath = devicePath;
            Devices = new List<Device>();
            var handle = IntPtr.Zero;

            if (devicePath == null)
                return;

            try
            {
                handle = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero,
                    UsbApi.OpenExisting, 0, IntPtr.Zero);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    GetDescriptorFromNodeConnection(adapterNumber, handle);

                    // The iManufacturer, iProduct and iSerialNumber entries in the
                    // device descriptor are really just indexes.  So, we have to 
                    // request a string descriptor to get the values for those strings.
                    if (DeviceDescriptor != null && DeviceDescriptor.iManufacturer > 0)
                        Manufacturer =
                            UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.iManufacturer);

                    if (DeviceDescriptor != null && DeviceDescriptor.iSerialNumber > 0)
                        SerialNumber =
                            UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.iSerialNumber);

                    if (DeviceDescriptor != null && DeviceDescriptor.iProduct > 0)
                        Product = UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.iProduct);

                    GetDriverKeyName(adapterNumber, handle);
                }
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    KernelApi.CloseHandle(handle);
            }
        }

        private void GetDescriptorFromNodeConnection(uint adapterNumber, IntPtr handle)
        {
            // We use this to zero fill a buffer
            var nullString = new string((char) 0, UsbApi.MaxBufferSize / Marshal.SystemDefaultCharSize);

            var nBytes = UsbApi.MaxBufferSize;
            // build a request for string descriptor
            var request1 =
                new UsbApi.UsbDescriptorRequest
                {
                    ConnectionIndex = adapterNumber,
                    SetupPacket =
                    {
                        Value = UsbApi.UsbConfigurationDescriptorType << 8,
                        Index = 0x409 // Language Code
                    }
                };

            request1.SetupPacket.Length = (short) (nBytes - Marshal.SizeOf(request1));

            // Geez, I wish C# had a Marshal.MemSet() method
            var ptrRequest = IntPtr.Zero;

            try
            {
                ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                Marshal.StructureToPtr(request1, ptrRequest, true);

                // Use an IOCTL call to request the String Descriptor
                if (KernelApi.DeviceIoControl(handle,
                    UsbIoControl.IoctlUsbGetDescriptorFromNodeConnection,
                    ptrRequest,
                    nBytes, ptrRequest, nBytes, out _, IntPtr.Zero))
                {
                    var ptr = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request1));

                    var configurationDescriptor = (UsbSpec.UsbConfigurationDescriptor)Marshal.PtrToStructure(ptr,
                        typeof(UsbSpec.UsbConfigurationDescriptor));

                    ConfigurationDescriptors.Add(configurationDescriptor);

                    var p = (long)ptr;
                    p += Marshal.SizeOf(configurationDescriptor) - 1;
                    ptr = (IntPtr)p;

                    for (var i = 0; i < configurationDescriptor.bNumInterfaces; i++)
                    {
                        var interfaceDescriptor =
                            (UsbSpec.UsbInterfaceDescriptor)Marshal.PtrToStructure(ptr,
                                typeof(UsbSpec.UsbInterfaceDescriptor));

                        InterfaceDescriptors.Add(interfaceDescriptor);

                        p = (long)ptr;
                        p += Marshal.SizeOf(interfaceDescriptor);

                        if (interfaceDescriptor.bInterfaceClass ==
                            UsbSpec.UsbDeviceClass.UsbDeviceClassHumanInterface)
                        {
                            ptr = (IntPtr)p;
                            for (var k = 0; k < interfaceDescriptor.bInterfaceSubClass; k++)
                            {
                                var hdiDescriptor =
                                    (UsbDesc.HidDescriptor)Marshal.PtrToStructure(ptr,
                                        typeof(UsbDesc.HidDescriptor));

                                HidDescriptors.Add(hdiDescriptor);

                                p = (long)ptr;
                                p += Marshal.SizeOf(hdiDescriptor);
                                p--;
                            }
                        }

                        ptr = (IntPtr)p;
                        for (var j = 0; j < interfaceDescriptor.bNumEndpoints; j++)
                        {
                            var endpointDescriptor1 =
                                (UsbSpec.UsbEndpointDescriptor)Marshal.PtrToStructure(ptr,
                                    typeof(UsbSpec.UsbEndpointDescriptor));

                            EndpointDescriptors.Add(endpointDescriptor1);

                            p = (long)ptr;
                            p += Marshal.SizeOf(endpointDescriptor1) - 1;
                            ptr = (IntPtr)p;
                        }
                    }
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetDescriptorFromNodeConnection)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if(ptrRequest != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrRequest);
            }
        }

        private void GetDriverKeyName(uint adapterNumber, IntPtr handle)
        {
            // Get the Driver Key Name (usefull in locating a device)
            var driverKey =
                new UsbApi.UsbNodeConnectionDriverkeyName {ConnectionIndex = adapterNumber};
            int nBytes = Marshal.SizeOf(driverKey);
            IntPtr ptrDriverKey = IntPtr.Zero;

            try
            {
                ptrDriverKey = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(driverKey, ptrDriverKey, true);

                // Use an IOCTL call to request the Driver Key Name
                if (KernelApi.DeviceIoControl(handle, UsbIoControl.IoctlUsbGetNodeConnectionDriverkeyName,
                    ptrDriverKey,
                    nBytes, ptrDriverKey, nBytes, out _, IntPtr.Zero))
                {
                    driverKey = (UsbApi.UsbNodeConnectionDriverkeyName)Marshal.PtrToStructure(ptrDriverKey,
                        typeof(UsbApi.UsbNodeConnectionDriverkeyName));
                    DriverKey = driverKey.DriverKeyName;

                    // use the DriverKeyName to get the Device Description, Instance ID, and DevicePath for devices(not hubs)
                    DeviceDescription = GetDescriptionByKeyName(DriverKey);
                    InstanceId = GetInstanceIdByKeyName(DriverKey);
                    if (!IsHub)
                    {
                        // Get USB DevicePath, or HID DevicePath, for use with CreateFile()
                        var devPath = GetDevicePathByKeyName(DriverKey);
                        if (devPath.Length > 0)
                        {
                            DevicePath = devPath; // Start with USB DevicePath
                            if (DeviceDescriptor != null)
                            {
                                // Replace USB DevicePath with HidDevicePath if VID, PID, and SerialNumber match
                                var tmp = GetHidDevicePath(DeviceDescriptor);
                                if (tmp.Length > 0)
                                {
                                    // This USB device is a HID device, use the HID device name.
                                    // Also save the underlying USB device name to test with when
                                    // USB devices are plugged in.
                                    UsbDevicePath = DevicePath; // Saved underlying USB device name
                                    DevicePath = tmp; // HID device name
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (ptrDriverKey != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrDriverKey);
                }
            }
        }

        private static string UsbDescriptorRequestString(IntPtr handle, uint adapterNumber, byte descriptorIndex)
        {
            var nullString = new string((char) 0, UsbApi.MaxBufferSize / Marshal.SystemDefaultCharSize);
            var nBytes = UsbApi.MaxBufferSize;
            var result = string.Empty;
            var ptrRequest = IntPtr.Zero;

            try
            {
                // Build a request for string descriptor.
                var request =
                    new UsbApi.UsbDescriptorRequest
                    {
                        ConnectionIndex = adapterNumber,
                        SetupPacket =
                        {
                            Value = (short) ((UsbApi.UsbStringDescriptorType << 8) +
                                             descriptorIndex),
                            Index = 0x409 // Language Code
                        }
                    };
                request.SetupPacket.Length = (short) (nBytes - Marshal.SizeOf(request));

                // Geez, I wish C# had a Marshal.MemSet() method.
                ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                Marshal.StructureToPtr(request, ptrRequest, true);

                // Use an IOCTL call to request the string descriptor.
                if (KernelApi.DeviceIoControl(handle, UsbIoControl.IoctlUsbGetDescriptorFromNodeConnection, ptrRequest,
                    nBytes, ptrRequest, nBytes, out _, IntPtr.Zero))
                {
                    // The location of the string descriptor is immediately after
                    // the Request structure.  Because this location is not "covered"
                    // by the structure allocation, we're forced to zero out this
                    // chunk of memory by using the StringToHGlobalAuto() hack above
                    var ptrStringDesc = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request));
                    var stringDesc =
                        (UsbSpec.UsbStringDescriptor) Marshal.PtrToStructure(ptrStringDesc,
                            typeof(UsbSpec.UsbStringDescriptor));

                    result = stringDesc.String;
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetDescriptorFromNodeConnection)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (ptrRequest != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrRequest);
            }

            return result;
        }

        /// <summary>
        ///     Gets the name of the description by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        protected static string GetDescriptionByKeyName(string driverKeyName)
        {
            var descriptionkeyname = string.Empty;
            var devEnum = UsbApi.RegstrKeyUsb;

            var handle = IntPtr.Zero;
            var ptr = IntPtr.Zero;

            try
            {
                // Use the "enumerator form" of the SetupDiGetClassDevs API 
                // to generate a list of all USB devices
                handle =
                    UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfAllclasses);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                    var success = true;

                    for (var i = 0; success; i++)
                    {
                        // Create a device interface data structure.
                        var deviceInterfaceData = new UsbApi.SpDevinfoData();
                        deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInterfaceData);
                        if (success)
                        {
                            var requiredSize = -1;
                            var regType = UsbApi.RegSz;
                            var keyName = string.Empty;

                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInterfaceData,
                                (int) UsbApi.Spdrp.SpdrpDriver, ref regType, ptr, UsbApi.MaxBufferSize,
                                ref requiredSize))
                                keyName = Marshal.PtrToStringAuto(ptr);

                            // Is it a match?
                            if (keyName == driverKeyName)
                            {
                                if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInterfaceData,
                                    (int) UsbApi.Spdrp.SpdrpDevicedesc, ref regType, ptr, UsbApi.MaxBufferSize,
                                    ref requiredSize))
                                    descriptionkeyname = Marshal.PtrToStringAuto(ptr);

                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }

            return descriptionkeyname;
        }

        /// <summary>
        ///     Gets the name of the instance ID by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        private static string GetInstanceIdByKeyName(string driverKeyName)
        {
            var descriptionkeyname = string.Empty;
            var devEnum = UsbApi.RegstrKeyUsb;
            var handle = IntPtr.Zero;
            var ptr = IntPtr.Zero;

            try
            {
                // Use the "enumerator form" of the SetupDiGetClassDevs API 
                // to generate a list of all USB devices
                handle =
                    UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfAllclasses);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                    var success = true;

                    for (var i = 0; success; i++)
                    {
                        // Create a device interface data structure.
                        var deviceInterfaceData = new UsbApi.SpDevinfoData();
                        deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInterfaceData);
                        if (success)
                        {
                            var requiredSize = -1;
                            var regType = UsbApi.RegSz;
                            var keyName = string.Empty;

                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInterfaceData,
                                (int) UsbApi.Spdrp.SpdrpDriver, ref regType, ptr, UsbApi.MaxBufferSize,
                                ref requiredSize))
                                keyName = Marshal.PtrToStringAuto(ptr);

                            // is it a match?
                            if (keyName == driverKeyName)
                            {
                                var nBytes = UsbApi.MaxBufferSize;
                                var sb = new StringBuilder(nBytes);
                                UsbApi.SetupDiGetDeviceInstanceId(handle, ref deviceInterfaceData, sb, nBytes,
                                    out requiredSize);
                                descriptionkeyname = sb.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }

            return descriptionkeyname;
        }

        /// <summary>
        ///     Gets the DevicePath (usable with CreateFile) by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        private static string GetDevicePathByKeyName(string driverKeyName)
        {
            var devicePathName = string.Empty;
            var handle = IntPtr.Zero;
            var ptr = IntPtr.Zero;

            try
            {
                // Generate a list of all USB devices
                var guidDevInterfaceUsbDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
                handle = UsbApi.SetupDiGetClassDevs(ref guidDevInterfaceUsbDevice, 0, IntPtr.Zero,
                    UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                    var success = true;

                    for (var i = 0; success; i++)
                    {
                        // Create a device info data structure.
                        var deviceInfoData = new UsbApi.SpDevinfoData();
                        deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);

                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInfoData);
                        if (success)
                        {
                            var requiredSize = -1;
                            var regType = UsbApi.RegSz;
                            var keyName = string.Empty;

                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handle, ref deviceInfoData,
                                (int) UsbApi.Spdrp.SpdrpDriver,
                                ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                                keyName = Marshal.PtrToStringAuto(ptr);

                            // is it a match?
                            if (keyName == driverKeyName)
                            {
                                // create a Device Interface Data structure
                                var deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                                deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                                if (UsbApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero,
                                    ref guidDevInterfaceUsbDevice,
                                    i, ref deviceInterfaceData))
                                {
                                    // Build a device interface detail data structure.
                                    var deviceInterfaceDetailData =
                                        new UsbApi.SpDeviceInterfaceDetailData
                                        {
                                            CbSize = 4 + Marshal.SystemDefaultCharSize
                                        };

                                    // Now we can get some more detailed informations.
                                    var nRequiredSize = 0;
                                    const int nBytes = UsbApi.MaxBufferSize;
                                    if (UsbApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData,
                                        ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                                        devicePathName = deviceInterfaceDetailData.DevicePath;

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (ptr == IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle == IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }

            return devicePathName;
        }

        /// <summary>
        ///     Gets the Hid DevicePath (usable with CreateFile) by Vid, Pid, and SerialNumber.
        /// </summary>
        /// <param name="deviceDescriptor">VID.</param>
        /// <returns></returns>
        private string GetHidDevicePath(UsbSpec.UsbDeviceDescriptor deviceDescriptor)
        {
            var hidDevicePath = string.Empty;

            var handle = IntPtr.Zero;
            var ptr = IntPtr.Zero;

            try
            {
                // Generate a list of all HID devices
                Guid guidHid;
                HidApi.HidD_GetHidGuid(
                    out guidHid); // next, get the GUID from Windows that it uses to represent the HID USB interface

                handle = UsbApi.SetupDiGetClassDevs(ref guidHid, 0, IntPtr.Zero,
                    UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                    var success = true;
                    for (var i = 0; success; i++)
                    {
                        // Create a device info data structure.
                        var deviceInfoData = new UsbApi.SpDevinfoData();
                        deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);
                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInfoData);
                        if (success)
                        {
                            // Create a device interface data structure.
                            var deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                            deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                            // Start the enumeration.
                            success = UsbApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref guidHid,
                                i, ref deviceInterfaceData);
                            if (success)
                            {
                                // Build a device interface detail data structure.
                                var deviceInterfaceDetailData =
                                    new UsbApi.SpDeviceInterfaceDetailData {CbSize = 4 + Marshal.SystemDefaultCharSize};

                                // Now we can get some more detailed informations.
                                var nRequiredSize = 0;
                                const int nBytes = UsbApi.MaxBufferSize;
                                if (UsbApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData,
                                    ref deviceInterfaceDetailData,
                                    nBytes, ref nRequiredSize,
                                    ref deviceInfoData))
                                {
                                    var strSearch =
                                        $"vid_{deviceDescriptor.idVendor:x4}&pid_{deviceDescriptor.idProduct:x4}";
                                    if (deviceInterfaceDetailData.DevicePath.Contains(strSearch) &&
                                        HidSerialNumberMatches(deviceInterfaceDetailData.DevicePath))
                                    {
                                        Debug.WriteLine($"HidPath:{deviceInterfaceDetailData.DevicePath}");
                                        hidDevicePath = deviceInterfaceDetailData.DevicePath;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);

                if (handle != IntPtr.Zero)
                    UsbApi.SetupDiDestroyDeviceInfoList(handle);
            }

            return hidDevicePath;
        }

        private bool HidSerialNumberMatches(string hidDevicePath)
        {
            // kludge: (uint) cast used to select SafeHandle CreateFile() method
            var hnd = KernelApi.CreateFile(hidDevicePath,
                UsbApi.GenericWrite | UsbApi.GenericRead,
                UsbApi.FileShareRead | UsbApi.FileShareWrite,
                IntPtr.Zero,
                UsbApi.OpenExisting,
                (uint) 0, IntPtr.Zero);
            if (hnd.IsInvalid)
                return false;
            try
            {
                var serialNumber = new StringBuilder(HidApi.HidStringLength);
                if (HidApi.HidD_GetSerialNumberString(hnd, serialNumber, serialNumber.Capacity))
                    return serialNumber.ToString() == SerialNumber;

                return false;
            }
            finally
            {
                hnd.Close();
                hnd.Dispose();
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var device in Devices)
                    {
                        device.Dispose();
                    }
                }

                disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
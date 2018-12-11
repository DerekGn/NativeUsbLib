using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Collections.ObjectModel;
using System.Diagnostics;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    /// abstract class of al usb devices
    /// </summary>
    public abstract class Device
    {
        #region fields

        /// <summary>
        /// Gets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public Device Parent { get; protected set; }

        /// <summary>
        /// The childs.
        /// </summary>
        protected List<Device> Devices = null;

        /// <summary>
        /// Gets the childs.
        /// </summary>
        /// <value>The childs.</value>
        public ReadOnlyCollection<Device> ChildDevices => new ReadOnlyCollection<Device>(Devices);

        private UsbApi.UsbNodeConnectionInformationEx m_NodeConnectionInfo;

        /// <summary>
        /// Gets or sets the node connection info.
        /// </summary>
        /// <value>The node connection info.</value>
        public UsbApi.UsbNodeConnectionInformationEx NodeConnectionInfo
        {
            get => m_NodeConnectionInfo;
            set
            {
                m_NodeConnectionInfo = value;
                AdapterNumber = NodeConnectionInfo.ConnectionIndex;
                UsbApi.UsbConnectionStatus status = NodeConnectionInfo.ConnectionStatus;
                Status = status.ToString();
                UsbApi.UsbDeviceSpeed speed = NodeConnectionInfo.Speed;
                Speed = speed.ToString();
                IsConnected = (NodeConnectionInfo.ConnectionStatus == UsbApi.UsbConnectionStatus.DeviceConnected);
                IsHub = Convert.ToBoolean(NodeConnectionInfo.DeviceIsHub);
            }
        }

        /// <summary>
        /// Gets or sets the device descriptor.
        /// </summary>
        /// <value>The device descriptor.</value>
        public UsbApi.UsbDeviceDescriptor DeviceDescriptor { get; set; }


        /// <summary>
        /// Gets the configuration descriptor.
        /// </summary>
        /// <value>The configuration descriptor.</value>
        public List<UsbApi.UsbConfigurationDescriptor> ConfigurationDescriptor { get; } = null;

        /// <summary>
        /// Gets the interface descriptor.
        /// </summary>
        /// <value>The interface descriptor.</value>
        public List<UsbApi.UsbInterfaceDescriptor> InterfaceDescriptor { get; } = null;

        /// <summary>
        /// Gets the endpoint descriptor.
        /// </summary>
        /// <value>The endpoint descriptor.</value>
        public List<UsbApi.UsbEndpointDescriptor> EndpointDescriptor { get; } = null;

        /// <summary>
        /// Gets the hdi descriptor.
        /// </summary>
        /// <value>The hdi descriptor.</value>
        public List<HidApi.HidDescriptor> HdiDescriptor { get; } = null;

        /// <summary>
        /// Gets the device path.
        /// </summary>
        /// <value>The device path.</value>
        public string DevicePath { get; protected set; }

        /// <summary>
        /// Gets the underlying USB device path for a HID device.
        /// </summary>
        /// <value>The underlying USB device path if a HID device, otherwise an empty string.</value>
        public string UsbDevicePath { get; } = string.Empty;

        /// <summary>
        /// Gets the driver key.
        /// </summary>
        /// <value>The driver key.</value>
        public string DriverKey { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the device description.
        /// </summary>
        /// <value>The device description.</value>
        public string DeviceDescription { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is hub.
        /// </summary>
        /// <value><c>true</c> if this instance is hub; otherwise, <c>false</c>.</value>
        public bool IsHub { get; set; } = false;

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the speed.
        /// </summary>
        /// <value>The speed.</value>
        public string Speed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the adapter number.
        /// </summary>
        /// <value>The adapter number.</value>
        public int AdapterNumber { get; set; } = 0;

        /// <summary>
        /// Gets the manufacturer.
        /// </summary>
        /// <value>The manufacturer.</value>
        public string Manufacturer { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the instance id.
        /// </summary>
        /// <value>The instance id.</value>
        public string InstanceId { get; } = string.Empty;

        /// <summary>
        /// Gets the serial number.
        /// </summary>
        /// <value>The serial number.</value>
        public string SerialNumber { get; } = string.Empty;

        /// <summary>
        /// Gets the product.
        /// </summary>
        /// <value>The product.</value>
        public string Product { get; } = string.Empty;

        #endregion

        #region constructor/destructor

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="adapterNumber">The adapter number.</param>
        /// <param name="devicePath">The device path.</param>
        protected Device(Device parent, UsbApi.UsbDeviceDescriptor deviceDescriptor, int adapterNumber,
            string devicePath)
        {
            Parent = parent;
            AdapterNumber = adapterNumber;
            DeviceDescriptor = deviceDescriptor;
            DevicePath = devicePath;
            Devices = new List<Device>();
            IntPtr handle = IntPtr.Zero;

            if (devicePath == null)
                return;

            try
            {
                handle = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero,
                UsbApi.OpenExisting, 0, IntPtr.Zero);
                if (handle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    // We use this to zero fill a buffer
                    string nullString = new string((char)0, UsbApi.MaxBufferSize / Marshal.SystemDefaultCharSize);

                    int nBytesReturned;
                    int nBytes = UsbApi.MaxBufferSize;
                    // build a request for string descriptor
                    UsbApi.UsbDescriptorRequest request1 =
                        new UsbApi.UsbDescriptorRequest
                        {
                            ConnectionIndex = adapterNumber,
                            SetupPacket =
                            {
                            WValue = (short) UsbApi.UsbConfigurationDescriptorType << 8,
                            WIndex = 0x409 // Language Code
                        }
                        };

                    request1.SetupPacket.WLength = (short)(nBytes - Marshal.SizeOf(request1));

                    // Geez, I wish C# had a Marshal.MemSet() method
                    IntPtr ptrRequest1 = Marshal.StringToHGlobalAuto(nullString);
                    Marshal.StructureToPtr(request1, ptrRequest1, true);

                    // Use an IOCTL call to request the String Descriptor
                    if (KernelApi.DeviceIoControl(handle, UsbApi.IoctlUsbGetDescriptorFromNodeConnection, ptrRequest1,
                        nBytes, ptrRequest1, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        IntPtr ptr = new IntPtr(ptrRequest1.ToInt64() + Marshal.SizeOf(request1));

                        var configurationDescriptor = (UsbApi.UsbConfigurationDescriptor)Marshal.PtrToStructure(ptr,
                            typeof(UsbApi.UsbConfigurationDescriptor));

                        if (ConfigurationDescriptor == null)
                        {
                            ConfigurationDescriptor = new List<UsbApi.UsbConfigurationDescriptor>
                        {
                            configurationDescriptor
                        };
                        }
                        else
                            ConfigurationDescriptor.Add(configurationDescriptor);

                        long p = (long)ptr;
                        p += Marshal.SizeOf(configurationDescriptor) - 1;
                        ptr = (IntPtr)p;

                        for (int i = 0; i < configurationDescriptor.BNumInterface; i++)
                        {
                            UsbApi.UsbInterfaceDescriptor interfaceDescriptor =
                                (UsbApi.UsbInterfaceDescriptor)Marshal.PtrToStructure(ptr,
                                    typeof(UsbApi.UsbInterfaceDescriptor));

                            if (InterfaceDescriptor == null)
                            {
                                InterfaceDescriptor = new List<UsbApi.UsbInterfaceDescriptor> {interfaceDescriptor};
                            }
                            else
                                InterfaceDescriptor.Add(interfaceDescriptor);

                            p = (long)ptr;
                            p += Marshal.SizeOf(interfaceDescriptor);

                            if (interfaceDescriptor.BInterfaceClass == 0x03)
                            {
                                ptr = (IntPtr)p;
                                for (int k = 0; k < interfaceDescriptor.BInterfaceSubClass; k++)
                                {
                                    HidApi.HidDescriptor hdiDescriptor =
                                        (HidApi.HidDescriptor)Marshal.PtrToStructure(ptr, typeof(HidApi.HidDescriptor));

                                    if (HdiDescriptor == null)
                                    {
                                        HdiDescriptor = new List<HidApi.HidDescriptor>
                                    {
                                        hdiDescriptor
                                    };
                                    }
                                    else
                                        HdiDescriptor.Add(hdiDescriptor);

                                    p = (long)ptr;
                                    p += Marshal.SizeOf(hdiDescriptor);
                                    p--;
                                }
                            }

                            ptr = (IntPtr)p;
                            for (int j = 0; j < interfaceDescriptor.BNumEndpoints; j++)
                            {
                                UsbApi.UsbEndpointDescriptor endpointDescriptor1 =
                                    (UsbApi.UsbEndpointDescriptor)Marshal.PtrToStructure(ptr,
                                        typeof(UsbApi.UsbEndpointDescriptor));
                                if (EndpointDescriptor == null)
                                {
                                    EndpointDescriptor = new List<UsbApi.UsbEndpointDescriptor>
                                {
                                    endpointDescriptor1
                                };
                                }
                                else
                                    EndpointDescriptor.Add(endpointDescriptor1);

                                p = (long)ptr;
                                p += Marshal.SizeOf(endpointDescriptor1) - 1;
                                ptr = (IntPtr)p;
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceError(
                            $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbApi.IoctlUsbGetDescriptorFromNodeConnection)}] Result: [{KernelApi.GetLastError():X}]");
                    }

                    Marshal.FreeHGlobal(ptrRequest1);

                    // The iManufacturer, iProduct and iSerialNumber entries in the
                    // device descriptor are really just indexes.  So, we have to 
                    // request a string descriptor to get the values for those strings.
                    if (DeviceDescriptor != null && DeviceDescriptor.IManufacturer > 0)
                    {
                        Manufacturer = UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.IManufacturer);
                    }

                    if (DeviceDescriptor != null && DeviceDescriptor.ISerialNumber > 0)
                    {
                        SerialNumber = UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.ISerialNumber);
                    }

                    if (DeviceDescriptor != null && DeviceDescriptor.IProduct > 0)
                    {
                        Product = UsbDescriptorRequestString(handle, adapterNumber, DeviceDescriptor.ISerialNumber);
                    }

                    // Get the Driver Key Name (usefull in locating a device)
                    UsbApi.UsbNodeConnectionDriverkeyName driverKey =
                        new UsbApi.UsbNodeConnectionDriverkeyName { ConnectionIndex = adapterNumber };
                    nBytes = Marshal.SizeOf(driverKey);
                    IntPtr ptrDriverKey = Marshal.AllocHGlobal(nBytes);
                    Marshal.StructureToPtr(driverKey, ptrDriverKey, true);

                    // Use an IOCTL call to request the Driver Key Name
                    if (KernelApi.DeviceIoControl(handle, UsbApi.IoctlUsbGetNodeConnectionDriverkeyName, ptrDriverKey,
                        nBytes, ptrDriverKey, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        driverKey = (UsbApi.UsbNodeConnectionDriverkeyName)Marshal.PtrToStructure(ptrDriverKey,
                            typeof(UsbApi.UsbNodeConnectionDriverkeyName));
                        DriverKey = driverKey.DriverKeyName;

                        // use the DriverKeyName to get the Device Description, Instance ID, and DevicePath for devices(not hubs)
                        DeviceDescription = GetDescriptionByKeyName(DriverKey);
                        InstanceId = GetInstanceIDByKeyName(DriverKey);
                        if (!IsHub)
                        {
                            // Get USB DevicePath, or HID DevicePath, for use with CreateFile()
                            string devPath = GetDevicePathByKeyName(DriverKey);
                            if (devPath.Length > 0)
                            {
                                DevicePath = devPath; // Start with USB DevicePath
                                if (DeviceDescriptor != null)
                                {
                                    // Replace USB DevicePath with HidDevicePath if VID, PID, and SerialNumber match
                                    string tmp = GetHidDevicePath(DeviceDescriptor);
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

                    Marshal.FreeHGlobal(ptrDriverKey);
                }
            }
            finally 
            {
                if(handle != IntPtr.Zero)
                    KernelApi.CloseHandle(handle);
            }
        }

        private static string UsbDescriptorRequestString(IntPtr handle, int adapterNumber, byte descriptorIndex)
        {
            string nullString = new string((char)0, UsbApi.MaxBufferSize / Marshal.SystemDefaultCharSize);
            int nBytes = UsbApi.MaxBufferSize;
            string result = string.Empty;
            IntPtr ptrRequest = IntPtr.Zero;
            
            try
            {
                // Build a request for string descriptor.
                UsbApi.UsbDescriptorRequest request =
                    new UsbApi.UsbDescriptorRequest
                    {
                        ConnectionIndex = adapterNumber,
                        SetupPacket =
                        {
                        WValue = (short) ((UsbApi.UsbStringDescriptorType << 8) +
                                          descriptorIndex),
                        WIndex = 0x409 // Language Code
                    }
                    };
                request.SetupPacket.WLength = (short)(nBytes - Marshal.SizeOf(request));

                // Geez, I wish C# had a Marshal.MemSet() method.
                ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                Marshal.StructureToPtr(request, ptrRequest, true);

                // Use an IOCTL call to request the string descriptor.
                if (KernelApi.DeviceIoControl(handle, UsbApi.IoctlUsbGetDescriptorFromNodeConnection, ptrRequest,
                    nBytes, ptrRequest, nBytes, out _, IntPtr.Zero))
                {
                    // The location of the string descriptor is immediately after
                    // the Request structure.  Because this location is not "covered"
                    // by the structure allocation, we're forced to zero out this
                    // chunk of memory by using the StringToHGlobalAuto() hack above
                    IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request));
                    UsbApi.UsbStringDescriptor stringDesc =
                        (UsbApi.UsbStringDescriptor)Marshal.PtrToStructure(ptrStringDesc,
                            typeof(UsbApi.UsbStringDescriptor));

                    result = stringDesc.BString;
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbApi.IoctlUsbGetDescriptorFromNodeConnection)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally 
            {
                if(ptrRequest != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrRequest);
            }

            return result;
        }

        #endregion

        #region destructor

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Device"/> is reclaimed by garbage collection.
        /// </summary>
        ~Device()
        {
            Devices?.Clear();
            Devices = null;
            Parent = null;
        }

        #endregion

        #endregion

        #region methodes

        #region methode GetDescriptionByKeyName

        /// <summary>
        /// Gets the name of the description by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        protected string GetDescriptionByKeyName(string driverKeyName)
        {
            string descriptionkeyname = string.Empty;
            string devEnum = UsbApi.RegstrKeyUsb;

            // Use the "enumerator form" of the SetupDiGetClassDevs API 
            // to generate a list of all USB devices
            IntPtr handel =
                UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfAllclasses);
            if (handel.ToInt64() != UsbApi.InvalidHandleValue)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device interface data structure.
                    UsbApi.SpDevinfoData deviceInterfaceData = new UsbApi.SpDevinfoData();
                    deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInterfaceData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.RegSz;
                        var keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                            UsbApi.SpdrpDriver, ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // Is it a match?
                        if (keyName == driverKeyName)
                        {
                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                                UsbApi.SpdrpDevicedesc, ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                                descriptionkeyname = Marshal.PtrToStringAuto(ptr);

                            break;
                        }
                    }
                }

                Marshal.FreeHGlobal(ptr);
                UsbApi.SetupDiDestroyDeviceInfoList(handel);
            }

            return descriptionkeyname;
        }

        #endregion

        #region methode GetInstanceIDByKeyName

        /// <summary>
        /// Gets the name of the instance ID by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        private string GetInstanceIDByKeyName(string driverKeyName)
        {
            string descriptionkeyname = string.Empty;
            string devEnum = UsbApi.RegstrKeyUsb;

            // Use the "enumerator form" of the SetupDiGetClassDevs API 
            // to generate a list of all USB devices
            IntPtr handel =
                UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DigcfPresent | UsbApi.DigcfAllclasses);
            if (handel.ToInt64() != UsbApi.InvalidHandleValue)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device interface data structure.
                    UsbApi.SpDevinfoData deviceInterfaceData = new UsbApi.SpDevinfoData();
                    deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInterfaceData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.RegSz;
                        var keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                            UsbApi.SpdrpDriver, ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // is it a match?
                        if (keyName == driverKeyName)
                        {
                            int nBytes = UsbApi.MaxBufferSize;
                            StringBuilder sb = new StringBuilder(nBytes);
                            UsbApi.SetupDiGetDeviceInstanceId(handel, ref deviceInterfaceData, sb, nBytes,
                                out requiredSize);
                            descriptionkeyname = sb.ToString();
                            break;
                        }
                    }
                }

                Marshal.FreeHGlobal(ptr);
                UsbApi.SetupDiDestroyDeviceInfoList(handel);
            }

            return descriptionkeyname;
        }

        #endregion

        /// <summary>
        /// Gets the DevicePath (usable with CreateFile) by key.
        /// </summary>
        /// <param name="driverKeyName">Name of the driver key.</param>
        /// <returns></returns>
        private string GetDevicePathByKeyName(string driverKeyName)
        {
            string devicePathName = string.Empty;

            // Generate a list of all USB devices
            var guidDevInterfaceUsbDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
            IntPtr handel = UsbApi.SetupDiGetClassDevs(ref guidDevInterfaceUsbDevice, 0, IntPtr.Zero,
                UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);
            if (handel.ToInt64() != UsbApi.InvalidHandleValue)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device info data structure.
                    var deviceInfoData = new UsbApi.SpDevinfoData();
                    deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInfoData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.RegSz;
                        string keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInfoData, UsbApi.SpdrpDriver,
                            ref regType, ptr, UsbApi.MaxBufferSize, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // is it a match?
                        if (keyName == driverKeyName)
                        {
                            // create a Device Interface Data structure
                            var deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                            deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                            if (UsbApi.SetupDiEnumDeviceInterfaces(handel, IntPtr.Zero, ref guidDevInterfaceUsbDevice,
                                i, ref deviceInterfaceData))
                            {
                                // Build a device interface detail data structure.
                                var deviceInterfaceDetailData =
                                    new UsbApi.SpDeviceInterfaceDetailData
                                    {
                                        CbSize = 4 + Marshal.SystemDefaultCharSize
                                    };

                                // Now we can get some more detailed informations.
                                int nRequiredSize = 0;
                                const int nBytes = UsbApi.MaxBufferSize;
                                if (UsbApi.SetupDiGetDeviceInterfaceDetail(handel, ref deviceInterfaceData,
                                    ref deviceInterfaceDetailData, nBytes, ref nRequiredSize, ref deviceInfoData))
                                {
                                    devicePathName = deviceInterfaceDetailData.DevicePath;
                                }

                                break;
                            }
                        }
                    }
                }

                Marshal.FreeHGlobal(ptr);
                UsbApi.SetupDiDestroyDeviceInfoList(handel);
            }

            return devicePathName;
        }

        /// <summary>
        /// Gets the Hid DevicePath (usable with CreateFile) by Vid, Pid, and SerialNumber.
        /// </summary>
        /// <param name="deviceDescriptor">VID.</param>
        /// <returns></returns>
        private string GetHidDevicePath(UsbApi.UsbDeviceDescriptor deviceDescriptor)
        {
            string hidDevicePath = string.Empty;

            // Generate a list of all HID devices
            Guid guidHid;
            HidApi.HidD_GetHidGuid(
                out guidHid); // next, get the GUID from Windows that it uses to represent the HID USB interface

            IntPtr handel = UsbApi.SetupDiGetClassDevs(ref guidHid, 0, IntPtr.Zero,
                UsbApi.DigcfPresent | UsbApi.DigcfDeviceinterface);
            if (handel.ToInt64() != UsbApi.InvalidHandleValue)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MaxBufferSize);
                bool success = true;
                for (int i = 0; success; i++)
                {
                    // Create a device info data structure.
                    var deviceInfoData = new UsbApi.SpDevinfoData();
                    deviceInfoData.CbSize = Marshal.SizeOf(deviceInfoData);
                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInfoData);
                    if (success)
                    {
                        // Create a device interface data structure.
                        var deviceInterfaceData = new UsbApi.SpDeviceInterfaceData();
                        deviceInterfaceData.CbSize = Marshal.SizeOf(deviceInterfaceData);

                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInterfaces(handel, IntPtr.Zero, ref guidHid,
                            i, ref deviceInterfaceData);
                        if (success)
                        {
                            // Build a device interface detail data structure.
                            var deviceInterfaceDetailData =
                                new UsbApi.SpDeviceInterfaceDetailData {CbSize = 4 + Marshal.SystemDefaultCharSize};

                            // Now we can get some more detailed informations.
                            int nRequiredSize = 0;
                            const int nBytes = UsbApi.MaxBufferSize;
                            if (UsbApi.SetupDiGetDeviceInterfaceDetail(handel, ref deviceInterfaceData,
                                ref deviceInterfaceDetailData,
                                nBytes, ref nRequiredSize,
                                ref deviceInfoData))
                            {
                                string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}",
                                    deviceDescriptor.IdVendor,
                                    deviceDescriptor.IdProduct);
                                if (deviceInterfaceDetailData.DevicePath.Contains(strSearch) &&
                                    HidSerialNumberMatches(deviceInterfaceDetailData.DevicePath))
                                {
                                    Debug.WriteLine(string.Format("HidPath:{0}",
                                        deviceInterfaceDetailData.DevicePath));
                                    hidDevicePath = deviceInterfaceDetailData.DevicePath;
                                    break;
                                }
                            }
                        }
                    }
                }

                Marshal.FreeHGlobal(ptr);
                UsbApi.SetupDiDestroyDeviceInfoList(handel);
            }

            return hidDevicePath;
        }

        bool HidSerialNumberMatches(string hidDevicePath)
        {
            // kludge: (uint) cast used to select SafeHandle CreateFile() method
            SafeFileHandle hnd = KernelApi.CreateFile(hidDevicePath,
                UsbApi.GenericWrite | UsbApi.GenericRead,
                UsbApi.FileShareRead | UsbApi.FileShareWrite,
                IntPtr.Zero,
                (uint) UsbApi.OpenExisting,
                (uint) 0, IntPtr.Zero);
            if (hnd.IsInvalid)
            {
                return false;
            }
            else
            {
                try
                {
                    var serialNumber = new StringBuilder(HidApi.HidStringLength);
                    if (HidApi.HidD_GetSerialNumberString(hnd, serialNumber, serialNumber.Capacity))
                    {
                        return serialNumber.ToString() == SerialNumber;
                    }

                    return false;
                }
                finally
                {
                    hnd.Close();
                    hnd.Dispose();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// static class to factory class to build the connected devices
    /// </summary>
    public static class DeviceFactory
    {
        /// <summary>
        /// Builds the device.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="portCount">The port count.</param>
        /// <param name="devicePath">The device path.</param>
        /// <returns>The device.</returns>
        public static Device BuildDevice(Device parent, int portCount, string devicePath)
        {
            Device device = null;

            int nBytesReturned;
            bool isConnected = false;

            // Open a handle to the Hub device
            IntPtr handel1 = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero,
                UsbApi.OpenExisting, 0, IntPtr.Zero);
            if (handel1.ToInt64() != UsbApi.InvalidHandleValue)
            {
                int nBytes = Marshal.SizeOf(typeof(UsbApi.UsbNodeConnectionInformationEx));
                IntPtr ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                UsbApi.UsbNodeConnectionInformationEx nodeConnection =
                    new UsbApi.UsbNodeConnectionInformationEx {ConnectionIndex = portCount};
                Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);

                if (KernelApi.DeviceIoControl(handel1, UsbApi.IoctlUsbGetNodeConnectionInformationEx,
                    ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    nodeConnection =
                        (UsbApi.UsbNodeConnectionInformationEx) Marshal.PtrToStructure(ptrNodeConnection,
                            typeof(UsbApi.UsbNodeConnectionInformationEx));
                    isConnected = (nodeConnection.ConnectionStatus == UsbApi.UsbConnectionStatus.DeviceConnected);
                }

                if (isConnected)
                {
                    if (nodeConnection.DeviceDescriptor.BDeviceClass == UsbApi.UsbDeviceClass.HubDevice)
                    {
                        nBytes = Marshal.SizeOf(typeof(UsbApi.UsbNodeConnectionName));
                        ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                        Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);

                        if (KernelApi.DeviceIoControl(handel1, UsbApi.IoctlUsbGetNodeConnectionName,
                            ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                        {
                            var nameConnection = (UsbApi.UsbNodeConnectionName) Marshal.PtrToStructure(
                                ptrNodeConnection,
                                typeof(UsbApi.UsbNodeConnectionName));
                            string name = @"\\?\" + nameConnection.NodeName;

                            device = new UsbHub(parent, nodeConnection.DeviceDescriptor, name)
                            {
                                NodeConnectionInfo = nodeConnection
                            };
                        }
                    }
                    else
                    {
                        device = new UsbDevice(parent, nodeConnection.DeviceDescriptor, portCount, devicePath)
                        {
                            NodeConnectionInfo = nodeConnection
                        };
                    }
                }
                else
                {
                    device = new UsbDevice(parent, null, portCount) {NodeConnectionInfo = nodeConnection};
                }

                Marshal.FreeHGlobal(ptrNodeConnection);
                KernelApi.CloseHandle(handel1);
            }

            return device;
        }
    }
}
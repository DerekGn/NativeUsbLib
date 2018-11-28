using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Collections.ObjectModel;

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

        private UsbApi.USB_NODE_CONNECTION_INFORMATION_EX m_NodeConnectionInfo;

        /// <summary>
        /// Gets or sets the node connection info.
        /// </summary>
        /// <value>The node connection info.</value>
        public UsbApi.USB_NODE_CONNECTION_INFORMATION_EX NodeConnectionInfo
        {
            get { return m_NodeConnectionInfo; }
            set
            {
                m_NodeConnectionInfo = value;
                AdapterNumber = NodeConnectionInfo.ConnectionIndex;
                UsbApi.USB_CONNECTION_STATUS status = NodeConnectionInfo.ConnectionStatus;
                Status = status.ToString();
                UsbApi.USB_DEVICE_SPEED speed = NodeConnectionInfo.Speed;
                Speed = speed.ToString();
                IsConnected = (NodeConnectionInfo.ConnectionStatus == UsbApi.USB_CONNECTION_STATUS.DeviceConnected);
                IsHub = Convert.ToBoolean(NodeConnectionInfo.DeviceIsHub);
            }
        }

        /// <summary>
        /// Gets or sets the device descriptor.
        /// </summary>
        /// <value>The device descriptor.</value>
        public UsbApi.USB_DEVICE_DESCRIPTOR DeviceDescriptor { get; set; }


        /// <summary>
        /// Gets the configuration descriptor.
        /// </summary>
        /// <value>The configuration descriptor.</value>
        public List<UsbApi.USB_CONFIGURATION_DESCRIPTOR> ConfigurationDescriptor { get; } = null;

        /// <summary>
        /// Gets the interface descriptor.
        /// </summary>
        /// <value>The interface descriptor.</value>
        public List<UsbApi.USB_INTERFACE_DESCRIPTOR> InterfaceDescriptor { get; } = null;

        /// <summary>
        /// Gets the endpoint descriptor.
        /// </summary>
        /// <value>The endpoint descriptor.</value>
        public List<UsbApi.USB_ENDPOINT_DESCRIPTOR> EndpointDescriptor { get; } = null;

        /// <summary>
        /// Gets the hdi descriptor.
        /// </summary>
        /// <value>The hdi descriptor.</value>
        public List<UsbApi.HID_DESCRIPTOR> HdiDescriptor { get; } = null;


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
        public string Name { get; set; } = string.Empty;

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
        public int AdapterNumber { get; set; } = -1;

        /// <summary>
        /// Gets the manufacturer.
        /// </summary>
        /// <value>The manufacturer.</value>
        public string Manufacturer { get; } = string.Empty;

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
        protected Device(Device parent, UsbApi.USB_DEVICE_DESCRIPTOR deviceDescriptor, int adapterNumber,
            string devicePath)
        {
            Parent = parent;
            AdapterNumber = adapterNumber;
            DeviceDescriptor = deviceDescriptor;
            DevicePath = devicePath;
            Devices = new List<Device>();

            if (devicePath == null)
                return;

            IntPtr handel = UsbApi.CreateFile(devicePath, UsbApi.GENERIC_WRITE, UsbApi.FILE_SHARE_WRITE, IntPtr.Zero,
                UsbApi.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handel.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                // We use this to zero fill a buffer
                string nullString = new string((char) 0, UsbApi.MAX_BUFFER_SIZE / Marshal.SystemDefaultCharSize);

                int nBytesReturned;
                int nBytes = UsbApi.MAX_BUFFER_SIZE;
                // build a request for string descriptor
                UsbApi.USB_DESCRIPTOR_REQUEST request1 =
                    new UsbApi.USB_DESCRIPTOR_REQUEST
                    {
                        ConnectionIndex = adapterNumber,
                        SetupPacket =
                        {
                            wValue = (short) UsbApi.USB_CONFIGURATION_DESCRIPTOR_TYPE << 8,
                            wIndex = 0x409 // Language Code
                        }
                    };
                // portCount;
                request1.SetupPacket.wLength = (short) (nBytes - Marshal.SizeOf(request1));
                request1.SetupPacket.wIndex = 0x409; // Language Code
                // Geez, I wish C# had a Marshal.MemSet() method
                IntPtr ptrRequest1 = Marshal.StringToHGlobalAuto(nullString);
                Marshal.StructureToPtr(request1, ptrRequest1, true);

                // Use an IOCTL call to request the String Descriptor
                if (UsbApi.DeviceIoControl(handel, UsbApi.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest1,
                    nBytes, ptrRequest1, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    IntPtr ptr = new IntPtr(ptrRequest1.ToInt64() + Marshal.SizeOf(request1));

                    var configurationDescriptor = (UsbApi.USB_CONFIGURATION_DESCRIPTOR) Marshal.PtrToStructure(ptr,
                        typeof(UsbApi.USB_CONFIGURATION_DESCRIPTOR));

                    if (ConfigurationDescriptor == null)
                    {
                        ConfigurationDescriptor = new List<UsbApi.USB_CONFIGURATION_DESCRIPTOR>();
                        ConfigurationDescriptor.Add(configurationDescriptor);
                    }
                    else
                        ConfigurationDescriptor.Add(configurationDescriptor);

                    long p = (long) ptr;
                    p += Marshal.SizeOf(configurationDescriptor) - 1;
                    ptr = (IntPtr) p;

                    for (int i = 0; i < configurationDescriptor.bNumInterface; i++)
                    {
                        UsbApi.USB_INTERFACE_DESCRIPTOR interfaceDescriptor =
                            (UsbApi.USB_INTERFACE_DESCRIPTOR) Marshal.PtrToStructure(ptr,
                                typeof(UsbApi.USB_INTERFACE_DESCRIPTOR));

                        if (InterfaceDescriptor == null)
                        {
                            InterfaceDescriptor = new List<UsbApi.USB_INTERFACE_DESCRIPTOR>();
                            InterfaceDescriptor.Add(interfaceDescriptor);
                        }
                        else
                            InterfaceDescriptor.Add(interfaceDescriptor);

                        p = (long) ptr;
                        p += Marshal.SizeOf(interfaceDescriptor);

                        if (interfaceDescriptor.bInterfaceClass == 0x03)
                        {
                            ptr = (IntPtr) p;
                            for (int k = 0; k < interfaceDescriptor.bInterfaceSubClass; k++)
                            {
                                UsbApi.HID_DESCRIPTOR hdiDescriptor =
                                    (UsbApi.HID_DESCRIPTOR) Marshal.PtrToStructure(ptr, typeof(UsbApi.HID_DESCRIPTOR));

                                if (HdiDescriptor == null)
                                {
                                    HdiDescriptor = new List<UsbApi.HID_DESCRIPTOR>
                                    {
                                        hdiDescriptor
                                    };
                                }
                                else
                                    HdiDescriptor.Add(hdiDescriptor);

                                p = (long) ptr;
                                p += Marshal.SizeOf(hdiDescriptor);
                                p--;
                            }
                        }

                        ptr = (IntPtr) p;
                        for (int j = 0; j < interfaceDescriptor.bNumEndpoints; j++)
                        {
                            UsbApi.USB_ENDPOINT_DESCRIPTOR endpointDescriptor1 =
                                (UsbApi.USB_ENDPOINT_DESCRIPTOR) Marshal.PtrToStructure(ptr,
                                    typeof(UsbApi.USB_ENDPOINT_DESCRIPTOR));
                            if (EndpointDescriptor == null)
                            {
                                EndpointDescriptor = new List<UsbApi.USB_ENDPOINT_DESCRIPTOR>
                                {
                                    endpointDescriptor1
                                };
                            }
                            else
                                EndpointDescriptor.Add(endpointDescriptor1);

                            p = (long) ptr;
                            p += Marshal.SizeOf(endpointDescriptor1) - 1;
                            ptr = (IntPtr) p;
                        }
                    }
                }

                Marshal.FreeHGlobal(ptrRequest1);

                // The iManufacturer, iProduct and iSerialNumber entries in the
                // device descriptor are really just indexes.  So, we have to 
                // request a string descriptor to get the values for those strings.
                if (DeviceDescriptor != null && DeviceDescriptor.iManufacturer > 0)
                {
                    // Build a request for string descriptor.
                    UsbApi.USB_DESCRIPTOR_REQUEST request =
                        new UsbApi.USB_DESCRIPTOR_REQUEST
                        {
                            ConnectionIndex = adapterNumber,
                            SetupPacket =
                            {
                                wValue = (short) ((UsbApi.USB_STRING_DESCRIPTOR_TYPE << 8) +
                                                  DeviceDescriptor.iManufacturer),
                                wIndex = 0x409 // Language Code
                            }
                        };
                    request.SetupPacket.wLength = (short) (nBytes - Marshal.SizeOf(request));
                    
                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                    Marshal.StructureToPtr(request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor.
                    if (UsbApi.DeviceIoControl(handel, UsbApi.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest,
                        nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        // The location of the string descriptor is immediately after
                        // the Request structure.  Because this location is not "covered"
                        // by the structure allocation, we're forced to zero out this
                        // chunk of memory by using the StringToHGlobalAuto() hack above
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request));
                        UsbApi.USB_STRING_DESCRIPTOR stringDesc =
                            (UsbApi.USB_STRING_DESCRIPTOR) Marshal.PtrToStructure(ptrStringDesc,
                                typeof(UsbApi.USB_STRING_DESCRIPTOR));
                        Manufacturer = stringDesc.bString;
                    }

                    Marshal.FreeHGlobal(ptrRequest);
                }

                if (DeviceDescriptor != null && DeviceDescriptor.iSerialNumber > 0)
                {
                    // Build a request for string descriptor.
                    UsbApi.USB_DESCRIPTOR_REQUEST request =
                        new UsbApi.USB_DESCRIPTOR_REQUEST
                        {
                            ConnectionIndex = adapterNumber,
                            SetupPacket =
                            {
                                wValue = (short) ((UsbApi.USB_STRING_DESCRIPTOR_TYPE << 8) +
                                                  DeviceDescriptor.iSerialNumber),
                                wIndex = 0x409 // Language Code
                            }
                        };
                    request.SetupPacket.wLength = (short) (nBytes - Marshal.SizeOf(request));
                    
                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                    Marshal.StructureToPtr(request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor
                    if (UsbApi.DeviceIoControl(handel, UsbApi.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest,
                        nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        // The location of the string descriptor is immediately after the request structure.
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request));
                        UsbApi.USB_STRING_DESCRIPTOR stringDesc =
                            (UsbApi.USB_STRING_DESCRIPTOR) Marshal.PtrToStructure(ptrStringDesc,
                                typeof(UsbApi.USB_STRING_DESCRIPTOR));
                        SerialNumber = stringDesc.bString;
                    }

                    Marshal.FreeHGlobal(ptrRequest);
                }

                if (DeviceDescriptor != null && DeviceDescriptor.iProduct > 0)
                {
                    // Build a request for endpoint descriptor.
                    UsbApi.USB_DESCRIPTOR_REQUEST request =
                        new UsbApi.USB_DESCRIPTOR_REQUEST
                        {
                            ConnectionIndex = adapterNumber,
                            SetupPacket =
                            {
                                wValue = (short) ((UsbApi.USB_STRING_DESCRIPTOR_TYPE << 8) + DeviceDescriptor.iProduct),
                                wIndex = 0x409 // Language Code
                            }
                        };
                    request.SetupPacket.wLength = (short) (nBytes - Marshal.SizeOf(request));
                    
                    // Geez, I wish C# had a Marshal.MemSet() method.
                    IntPtr ptrRequest = Marshal.StringToHGlobalAuto(nullString);
                    Marshal.StructureToPtr(request, ptrRequest, true);

                    // Use an IOCTL call to request the string descriptor.
                    if (UsbApi.DeviceIoControl(handel, UsbApi.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, ptrRequest,
                        nBytes, ptrRequest, nBytes, out nBytesReturned, IntPtr.Zero))
                    {
                        // the location of the string descriptor is immediately after the Request structure
                        IntPtr ptrStringDesc = new IntPtr(ptrRequest.ToInt64() + Marshal.SizeOf(request));
                        UsbApi.USB_STRING_DESCRIPTOR stringDesc =
                            (UsbApi.USB_STRING_DESCRIPTOR) Marshal.PtrToStructure(ptrStringDesc,
                                typeof(UsbApi.USB_STRING_DESCRIPTOR));
                        Product = stringDesc.bString;
                    }

                    Marshal.FreeHGlobal(ptrRequest);
                }

                // Get the Driver Key Name (usefull in locating a device)
                UsbApi.USB_NODE_CONNECTION_DRIVERKEY_NAME driverKey =
                    new UsbApi.USB_NODE_CONNECTION_DRIVERKEY_NAME {ConnectionIndex = adapterNumber};
                nBytes = Marshal.SizeOf(driverKey);
                IntPtr ptrDriverKey = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(driverKey, ptrDriverKey, true);

                // Use an IOCTL call to request the Driver Key Name
                if (UsbApi.DeviceIoControl(handel, UsbApi.IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, ptrDriverKey,
                    nBytes, ptrDriverKey, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    driverKey = (UsbApi.USB_NODE_CONNECTION_DRIVERKEY_NAME) Marshal.PtrToStructure(ptrDriverKey,
                        typeof(UsbApi.USB_NODE_CONNECTION_DRIVERKEY_NAME));
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

            UsbApi.CloseHandle(handel);
        }

        #endregion

        #region destructor

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Device"/> is reclaimed by garbage collection.
        /// </summary>
        ~Device()
        {
            Devices.Clear();
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
            string devEnum = UsbApi.REGSTR_KEY_USB;

            // Use the "enumerator form" of the SetupDiGetClassDevs API 
            // to generate a list of all USB devices
            IntPtr handel =
                UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_ALLCLASSES);
            if (handel.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MAX_BUFFER_SIZE);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device interface data structure.
                    UsbApi.SP_DEVINFO_DATA deviceInterfaceData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInterfaceData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.REG_SZ;
                        var keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                            UsbApi.SPDRP_DRIVER, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // Is it a match?
                        if (keyName == driverKeyName)
                        {
                            if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                                UsbApi.SPDRP_DEVICEDESC, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
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
            string devEnum = UsbApi.REGSTR_KEY_USB;

            // Use the "enumerator form" of the SetupDiGetClassDevs API 
            // to generate a list of all USB devices
            IntPtr handel =
                UsbApi.SetupDiGetClassDevs(0, devEnum, IntPtr.Zero, UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_ALLCLASSES);
            if (handel.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MAX_BUFFER_SIZE);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device interface data structure.
                    UsbApi.SP_DEVINFO_DATA deviceInterfaceData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInterfaceData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.REG_SZ;
                        var keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInterfaceData,
                            UsbApi.SPDRP_DRIVER, ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // is it a match?
                        if (keyName == driverKeyName)
                        {
                            int nBytes = UsbApi.MAX_BUFFER_SIZE;
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
                UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_DEVICEINTERFACE);
            if (handel.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MAX_BUFFER_SIZE);
                bool success = true;

                for (int i = 0; success; i++)
                {
                    // Create a device info data structure.
                    var deviceInfoData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInfoData);
                    if (success)
                    {
                        int requiredSize = -1;
                        int regType = UsbApi.REG_SZ;
                        string keyName = string.Empty;

                        if (UsbApi.SetupDiGetDeviceRegistryProperty(handel, ref deviceInfoData, UsbApi.SPDRP_DRIVER,
                            ref regType, ptr, UsbApi.MAX_BUFFER_SIZE, ref requiredSize))
                            keyName = Marshal.PtrToStringAuto(ptr);

                        // is it a match?
                        if (keyName == driverKeyName)
                        {
                            // create a Device Interface Data structure
                            var deviceInterfaceData = new UsbApi.SP_DEVICE_INTERFACE_DATA();
                            deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                            if (UsbApi.SetupDiEnumDeviceInterfaces(handel, IntPtr.Zero, ref guidDevInterfaceUsbDevice,
                                i, ref deviceInterfaceData))
                            {
                                // Build a device interface detail data structure.
                                var deviceInterfaceDetailData =
                                    new UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA
                                    {
                                        cbSize = 4 + Marshal.SystemDefaultCharSize
                                    };

                                // Now we can get some more detailed informations.
                                int nRequiredSize = 0;
                                const int nBytes = UsbApi.MAX_BUFFER_SIZE;
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
        private string GetHidDevicePath(UsbApi.USB_DEVICE_DESCRIPTOR deviceDescriptor)
        {
            string hidDevicePath = string.Empty;

            // Generate a list of all HID devices
            Guid guidHid;
            UsbApi.HidD_GetHidGuid(
                out guidHid); // next, get the GUID from Windows that it uses to represent the HID USB interface

            IntPtr handel = UsbApi.SetupDiGetClassDevs(ref guidHid, 0, IntPtr.Zero,
                UsbApi.DIGCF_PRESENT | UsbApi.DIGCF_DEVICEINTERFACE);
            if (handel.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                IntPtr ptr = Marshal.AllocHGlobal(UsbApi.MAX_BUFFER_SIZE);
                bool success = true;
                for (int i = 0; success; i++)
                {
                    // Create a device info data structure.
                    var deviceInfoData = new UsbApi.SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
                    // Start the enumeration.
                    success = UsbApi.SetupDiEnumDeviceInfo(handel, i, ref deviceInfoData);
                    if (success)
                    {
                        // Create a device interface data structure.
                        var deviceInterfaceData = new UsbApi.SP_DEVICE_INTERFACE_DATA();
                        deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                        // Start the enumeration.
                        success = UsbApi.SetupDiEnumDeviceInterfaces(handel, IntPtr.Zero, ref guidHid,
                            i, ref deviceInterfaceData);
                        if (success)
                        {
                            // Build a device interface detail data structure.
                            var deviceInterfaceDetailData =
                                new UsbApi.SP_DEVICE_INTERFACE_DETAIL_DATA {cbSize = 4 + Marshal.SystemDefaultCharSize};
                            
                            // Now we can get some more detailed informations.
                            int nRequiredSize = 0;
                            const int nBytes = UsbApi.MAX_BUFFER_SIZE;
                            if (UsbApi.SetupDiGetDeviceInterfaceDetail(handel, ref deviceInterfaceData,
                                ref deviceInterfaceDetailData,
                                nBytes, ref nRequiredSize,
                                ref deviceInfoData))
                            {
                                string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}",
                                    deviceDescriptor.idVendor,
                                    deviceDescriptor.idProduct);
                                if (deviceInterfaceDetailData.DevicePath.Contains(strSearch))
                                {
                                    if (HidSerialNumberMatches(deviceInterfaceDetailData.DevicePath))
                                    {
                                        System.Diagnostics.Debug.WriteLine(string.Format("HidPath:{0}",
                                            deviceInterfaceDetailData.DevicePath));
                                        hidDevicePath = deviceInterfaceDetailData.DevicePath;
                                        break;
                                    }
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
            SafeFileHandle hnd = UsbApi.CreateFile(hidDevicePath,
                UsbApi.GENERIC_WRITE | UsbApi.GENERIC_READ,
                UsbApi.FILE_SHARE_READ | UsbApi.FILE_SHARE_WRITE,
                IntPtr.Zero,
                (uint) UsbApi.OPEN_EXISTING,
                (uint) 0, IntPtr.Zero);
            if (hnd.IsInvalid)
            {
                return false;
            }
            else
            {
                try
                {
                    var serialNumber = new StringBuilder(UsbApi.HidStringLength);
                    if (UsbApi.HidD_GetSerialNumberString(hnd, serialNumber, serialNumber.Capacity))
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
            IntPtr handel1 = UsbApi.CreateFile(devicePath, UsbApi.GENERIC_WRITE, UsbApi.FILE_SHARE_WRITE, IntPtr.Zero,
                UsbApi.OPEN_EXISTING, 0, IntPtr.Zero);
            if (handel1.ToInt64() != UsbApi.INVALID_HANDLE_VALUE)
            {
                int nBytes = Marshal.SizeOf(typeof(UsbApi.USB_NODE_CONNECTION_INFORMATION_EX));
                IntPtr ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                UsbApi.USB_NODE_CONNECTION_INFORMATION_EX nodeConnection =
                    new UsbApi.USB_NODE_CONNECTION_INFORMATION_EX {ConnectionIndex = portCount};
                Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);

                if (UsbApi.DeviceIoControl(handel1, UsbApi.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX,
                    ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    nodeConnection =
                        (UsbApi.USB_NODE_CONNECTION_INFORMATION_EX) Marshal.PtrToStructure(ptrNodeConnection,
                            typeof(UsbApi.USB_NODE_CONNECTION_INFORMATION_EX));
                    isConnected = (nodeConnection.ConnectionStatus == UsbApi.USB_CONNECTION_STATUS.DeviceConnected);
                }

                if (isConnected)
                {
                    if (nodeConnection.DeviceDescriptor.bDeviceClass == UsbApi.UsbDeviceClass.HubDevice)
                    {
                        nBytes = Marshal.SizeOf(typeof(UsbApi.USB_NODE_CONNECTION_NAME));
                        ptrNodeConnection = Marshal.AllocHGlobal(nBytes);
                        Marshal.StructureToPtr(nodeConnection, ptrNodeConnection, true);

                        if (UsbApi.DeviceIoControl(handel1, UsbApi.IOCTL_USB_GET_NODE_CONNECTION_NAME,
                            ptrNodeConnection, nBytes, ptrNodeConnection, nBytes, out nBytesReturned, IntPtr.Zero))
                        {
                            var nameConnection = (UsbApi.USB_NODE_CONNECTION_NAME) Marshal.PtrToStructure(ptrNodeConnection,
                                typeof(UsbApi.USB_NODE_CONNECTION_NAME));
                            string name = @"\\?\" + nameConnection.NodeName;
                            //this.childs.Add(new UsbHub(this, name, false));
                            device = new UsbHub(parent, nodeConnection.DeviceDescriptor, name)
                            {
                                NodeConnectionInfo = nodeConnection
                            };
                        }
                    }
                    else
                    {
                        //this.childs.Add(new UsbDevice(this, nodeConnection.DeviceDescriptor, portCount, devicePath));
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
                UsbApi.CloseHandle(handel1);
            }

            return device;
        }
    }
}
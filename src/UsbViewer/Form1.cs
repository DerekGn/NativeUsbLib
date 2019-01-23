using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NativeUsbLib;
using NativeUsbLib.WinApis;
using UsbViewer.Extensions;

namespace UsbViewer
{
    public partial class Form1 : Form
    {
        private readonly UsbBus _usbBus = new UsbBus();

        public Form1()
        {
            InitializeComponent();
            m_UsbTreeView.ImageList = new ImageList();
            m_UsbTreeView.ImageList.Images.Add(new Icon(".\\Icons\\monitor.ico"));
            m_UsbTreeView.ImageList.Images.Add(new Icon(".\\Icons\\port.ico"));
            m_UsbTreeView.ImageList.Images.Add(new Icon(".\\Icons\\hub.ico"));
            m_UsbTreeView.ImageList.Images.Add(new Icon(".\\Icons\\usb.ico"));
            m_UsbTreeView.ImageList.Images.Add(new Icon(".\\Icons\\bang.ico"));
            ReScanUsbBus();
        }

        #region build tree

        private void ReScanUsbBus()
        {
            if (m_UsbTreeView.Nodes[0].Nodes.Count > 0)
                m_UsbTreeView.Nodes[0].Nodes.Clear();

            _usbBus.Refresh();
            if (m_UsbTreeView.Nodes.Count > 0)
                foreach (var controller in _usbBus.Controller)
                    ShowController(m_UsbTreeView.Nodes[0], controller);

            m_UsbTreeView.ExpandAll();
        }

        private void ShowController(TreeNode node, UsbController controller)
        {
            if (controller != null)
            {
                var usbNode =
                    new UsbTreeNode(controller, DeviceTyp.Controller, m_DeviceContextMenuStrip)
                    {
                        Text = controller.DeviceDescription,
                        ImageIndex = 3,
                        SelectedImageIndex = 3
                    };
                node.Nodes.Add(usbNode);

                foreach (var hub in controller.Hubs) ShowHub(usbNode, hub);
            }
            else
            {
                Console.WriteLine("Controller  = null");
            }
        }

        private void ShowHub(TreeNode node, UsbHub hub)
        {
            if (hub != null)
            {
                UsbTreeNode usbNode;
                if (hub.IsRootHub)
                    usbNode = new UsbTreeNode(hub, DeviceTyp.RootHub, m_DeviceContextMenuStrip)
                    {
                        ImageIndex = 3,
                        SelectedImageIndex = 3,
                        Text = hub.DeviceDescription
                    };
                else
                    usbNode = new UsbTreeNode(hub, DeviceTyp.Hub, m_DeviceContextMenuStrip)
                    {
                        ImageIndex = 2,
                        SelectedImageIndex = 2,
                        Text = $"Port[{hub.AdapterNumber}] DeviceConnected: {hub.DeviceDescription}"
                    };
                node.Nodes.Add(usbNode);

                foreach (var device in hub.ChildDevices) ShowDevice(usbNode, device);
            }
            else
            {
                Console.WriteLine("\tHub = null");
            }
        }

        private void ShowDevice(TreeNode node, Device device)
        {
            if (device != null)
            {
                if (device is UsbHub hub)
                {
                    ShowHub(node, hub);
                }
                else
                {
                    var usbNode = new UsbTreeNode(device, DeviceTyp.Device, m_DeviceContextMenuStrip);
                    var s = "Port[" + device.AdapterNumber + "]";
                    if (device.IsConnected)
                    {
                        s += " DeviceConnected: " + device.DeviceDescription;

                        if (device.DriverKey == string.Empty)
                        {
                            usbNode.ImageIndex = 4;
                            usbNode.SelectedImageIndex = 4;
                        }
                        else
                        {
                            usbNode.ImageIndex = 3;
                            usbNode.SelectedImageIndex = 3;
                        }
                    }
                    else
                    {
                        s += " NoDeviceConnected";
                        usbNode.ImageIndex = 1;
                        usbNode.SelectedImageIndex = 1;
                    }

                    usbNode.Text = s;
                    node.Nodes.Add(usbNode);
                }
            }
            else
            {
                Console.WriteLine("Device = null");
            }
        }

        #endregion

        #region events

        private void m_UsbTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var sb = new StringBuilder();

            if (e.Node is UsbTreeNode usbNode)
            {
                if (usbNode.Type == DeviceTyp.Controller)
                {
                    var controller = usbNode.Device as UsbController;

                    AppendUsbController(sb, controller);
                }

                if ((usbNode.Type == DeviceTyp.Hub || usbNode.Type == DeviceTyp.RootHub) &&
                    usbNode.Device is UsbHub hub)
                    AppendUsbHub(sb, hub);

                if (usbNode.Type == DeviceTyp.Device || usbNode.Type == DeviceTyp.Hub)
                {
                    var device = usbNode.Device;
                    if (device.IsConnected)
                    {
                        var index = 1;

                        sb.AppendLine($"[Port{device.AdapterNumber}]  :  {device.DeviceDescription}\r\n");

                        AppendUsbPortConnectorProperties(sb, device.UsbPortConnectorProperties);

                        AppendNodeConnectionInfoExV2(sb, device.NodeConnectionInfoV2);

                        AppendDeviceInformation(sb, device);

                        AppendDeviceDescriptor(sb, device);

                        if (device.ConfigurationDescriptor != null)
                            foreach (var configurationDescriptor in device.ConfigurationDescriptor)
                            {
                                AppendConfigurationDescriptor(sb, index, configurationDescriptor);
                                index++;
                            }

                        if (device.InterfaceDescriptor != null)
                        {
                            index = 1;
                            foreach (var interfaceDescriptor in device.InterfaceDescriptor)
                            {
                                AppendInterfaceDescriptor(sb, index, interfaceDescriptor);
                                index++;
                            }
                        }

                        if (device.HdiDescriptor != null)
                        {
                            index = 1;
                            foreach (var hdiDescriptor in device.HdiDescriptor)
                            {
                                AppendHidDescriptor(sb, index, hdiDescriptor);
                                index++;
                            }
                        }

                        if (device.EndpointDescriptor != null)
                        {
                            index = 1;
                            foreach (var endpointDescriptor in device.EndpointDescriptor)
                            {
                                AppendEndpointDescriptor(sb, index, endpointDescriptor);
                                index++;
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine($"Connection Status: {device.Status}\n");
                    }
                }
            }

            m_RichTextBox.Clear();
            m_RichTextBox.Text = sb.ToString();
        }

        private void AppendDeviceDescriptor(StringBuilder builder, Device device)
        {
            UsbSpec.UsbDeviceDescriptor deviceDescriptor = device.DeviceDescriptor;
            UsbIoControl.UsbNodeConnectionInformationEx connectInfo = device.NodeConnectionInfo;

            bool tog = true;
            uint iaDcount = 0;

            builder.AppendLine("\r\n          ===>Device Descriptor<===");
            builder.AppendLine($"bLength:                           0x{deviceDescriptor.bLength:X02}");
            builder.AppendLine(
                $"bDescriptorType:                   0x{(int) deviceDescriptor.bDescriptorType:X02} -> {deviceDescriptor.bDescriptorType}");
            builder.AppendLine($"bcdUSB:                            0x{deviceDescriptor.bcdUSB:X04}");
            builder.Append(
                $"bDeviceClass:                      0x{(int) deviceDescriptor.bDeviceClass:X02} -> {deviceDescriptor.bDeviceClass}");

            // Quit on these device failures
            if (connectInfo.ConnectionStatus == UsbIoControl.UsbConnectionStatus.DeviceFailedEnumeration ||
                connectInfo.ConnectionStatus == UsbIoControl.UsbConnectionStatus.DeviceGeneralFailure)
                builder.AppendLine("\r\n*!*ERROR:  Device enumeration failure");

            // Is this an IAD device?
#warning TODO
            //iaDcount = IsIADDevice((PUSBDEVICEINFO)info);

            if (iaDcount > 0)
            {
                // this device configuration has 1 or more IAD descriptors
                if (connectInfo.DeviceDescriptor.bDeviceClass == UsbDesc.DeviceClassType.UsbMiscellaneousDevice)
                {
                    tog = false;

                    builder.AppendLine("  -> This is a Multi-interface Function Code Device");
                }
                else
                {
                    builder.AppendLine(
                        $"\r\n*!*ERROR: device class should be Multi-interface Function 0x{UsbDesc.DeviceClassType.UsbMiscellaneousDevice:X02}\r\n" +
                        "          When IAD descriptor is used");
                }
                // Is this a UVC device?
#warning TODO
                //g_chUVCversion = IsUVCDevice((PUSBDEVICEINFO)info);
            }
            else
            {
                // this is not an IAD device
                switch (connectInfo.DeviceDescriptor.bDeviceClass)
                {
                    case UsbDesc.DeviceClassType.UsbInterfaceClassDevice:
                        builder.AppendLine("  -> This is an Interface Class Defined Device");

                        break;

                    case UsbDesc.DeviceClassType.UsbCommunicationDevice:
                        tog = false;
                        builder.AppendLine("  -> This is a Communication Device");
                        break;

                    case UsbDesc.DeviceClassType.UsbHubDevice:
                        tog = false;
                        builder.AppendLine("  -> This is a HUB Device");
                        break;

                    case UsbDesc.DeviceClassType.UsbDiagnosticDevice:
                        tog = false;
                        builder.AppendLine("  -> This is a Diagnostic Device");
                        break;

                    case UsbDesc.DeviceClassType.UsbWirelessControllerDevice:
                        tog = false;

                        builder.AppendLine("  -> This is a Wireless Controller(Bluetooth) Device");
                        break;

                    case UsbDesc.DeviceClassType.UsbVendorSpecificDevice:
                        tog = false;
                        builder.AppendLine("  -> This is a Vendor Specific Device");
                        break;

                    case UsbDesc.DeviceClassType.UsbDeviceClassBillboard:
                        tog = false;

                        builder.AppendLine("  -> This is a billboard class device");
                        break;

                    case UsbDesc.DeviceClassType.UsbMiscellaneousDevice:
                        tog = false;
                        //@@TestCase A1.3
                        //@@ERROR
                        //@@Descriptor Field - bDeviceClass
                        //@@Multi-interface Function code used for non-IAD device
                        builder.AppendLine(
                            $"\r\n*!*ERROR:  Multi-interface Function code {connectInfo.DeviceDescriptor.bDeviceClass:D} used for device with no IAD descriptors");
                        break;

                    default:
                        //@@TestCase A1.4
                        //@@ERROR
                        //@@Descriptor Field - bDeviceClass
                        //@@An unknown device class has been defined
                        builder.AppendLine(
                            $"\r\n*!*ERROR:  unknown bDeviceClass {connectInfo.DeviceDescriptor.bDeviceClass:D}");
                        break;
                }
            }

            builder.Append($"bDeviceSubClass:                   0x{connectInfo.DeviceDescriptor.bDeviceSubClass}");

            // check the subclass
            if (iaDcount > 0)
            {
                // this device configuration has 1 or more IAD descriptors
                if (connectInfo.DeviceDescriptor.bDeviceSubClass == UsbDesc.UsbCommonSubClass)
                {
                    builder.AppendLine("  -> This is the Common Class Sub Class");
                }
                else
                {
                    //@@TestCase A1.5
                    //@@ERROR
                    //@@Descriptor Field - bDeviceSubClass
                    //@@An invalid device sub class used for Multi-interface Function (IAD) device
                    builder.AppendLine(
                        $"\r\n*!*ERROR: device SubClass should be USB Common Sub Class {UsbDesc.UsbCommonSubClass}\r\n" +
                        "          When IAD descriptor is used");
                }
            }
            else
            {
                // Not an IAD device, so all subclass values are invalid
                if (connectInfo.DeviceDescriptor.bDeviceSubClass > 0x00 &&
                    connectInfo.DeviceDescriptor.bDeviceSubClass < 0xFF)
                {
                    //@@TestCase A1.6
                    //@@ERROR
                    //@@Descriptor Field - bDeviceSubClass
                    //@@An invalid device sub class has been defined
                    builder.AppendLine(
                        $"\r\n*!*ERROR:  bDeviceSubClass of {connectInfo.DeviceDescriptor.bDeviceSubClass:D} is invalid");
                }
                else
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine(
                $"bDeviceProtocol:                   0x{connectInfo.DeviceDescriptor.bDeviceProtocol:X02}");

            // check the protocol
            if (iaDcount > 0)
            {
                // this device configuration has 1 or more IAD descriptors
                if (connectInfo.DeviceDescriptor.bDeviceProtocol == UsbDesc.UsbIadProtocol)
                {
                    builder.AppendLine("  -> This is the Interface Association Descriptor protocol");
                }
                else
                {
                    //@@TestCase A1.7
                    //@@ERROR
                    //@@Descriptor Field - bDeviceSubClass
                    //@@An invalid device sub class used for Multi-interface Function (IAD) device
                    builder.AppendLine(
                        $"\r\n*!*ERROR: device Protocol should be USB IAD Protocol {UsbDesc.UsbIadProtocol}\r\n" +
                        "          When IAD descriptor is used");
                }
            }
            else
            {
                // Not an IAD device, so all subclass values are invalid
                if (connectInfo.DeviceDescriptor.bDeviceProtocol > 0x00 &&
                    connectInfo.DeviceDescriptor.bDeviceProtocol < 0xFF && tog)
                {
                    //@@TestCase A1.8
                    //@@ERROR
                    //@@Descriptor Field - bDeviceProtocol
                    //@@An invalid device protocol has been defined
                    builder.AppendLine(
                        $"\r\n*!*ERROR:  bDeviceProtocol of {connectInfo.DeviceDescriptor.bDeviceProtocol} is invalid");
                }
                else
                {
                    builder.AppendLine("\r\n");
                }
            }

            builder.AppendLine(
                $"bMaxPacketSize0:                   0x{connectInfo.DeviceDescriptor.bMaxPacketSize0:X02}= ({connectInfo.DeviceDescriptor.bMaxPacketSize0}) Bytes");

            switch (connectInfo.Speed)
            {
                case UsbSpec.UsbDeviceSpeed.UsbLowSpeed:
                    if (connectInfo.DeviceDescriptor.bMaxPacketSize0 != 8)
                    {
                        //@@TestCase A1.9
                        //@@ERROR
                        //@@Descriptor Field - bMaxPacketSize0
                        //@@An invalid bMaxPacketSize0 has been defined for a low speed device
                        builder.AppendLine("*!*ERROR:  Low Speed Devices require bMaxPacketSize0 = 8\r\n");
                    }

                    break;
                case UsbSpec.UsbDeviceSpeed.UsbFullSpeed:
                    if (!(connectInfo.DeviceDescriptor.bMaxPacketSize0 == 8 ||
                          connectInfo.DeviceDescriptor.bMaxPacketSize0 == 16 ||
                          connectInfo.DeviceDescriptor.bMaxPacketSize0 == 32 ||
                          connectInfo.DeviceDescriptor.bMaxPacketSize0 == 64))
                    {
                        //@@TestCase A1.10
                        //@@ERROR
                        //@@Descriptor Field - bMaxPacketSize0
                        //@@An invalid bMaxPacketSize0 has been defined for a full speed device
                        builder.AppendLine(
                            "*!*ERROR:  Full Speed Devices require bMaxPacketSize0 = 8, 16, 32, or 64\r\n");
                    }

                    break;
                case UsbSpec.UsbDeviceSpeed.UsbHighSpeed:
                    if (connectInfo.DeviceDescriptor.bMaxPacketSize0 != 64)
                    {
                        //@@TestCase A1.11
                        //@@ERROR
                        //@@Descriptor Field - bMaxPacketSize0
                        //@@An invalid bMaxPacketSize0 has been defined for a high speed device
                        builder.AppendLine("*!*ERROR:  High Speed Devices require bMaxPacketSize0 = 64\r\n");
                    }

                    break;
                case UsbSpec.UsbDeviceSpeed.UsbSuperSpeed:
                    if (connectInfo.DeviceDescriptor.bMaxPacketSize0 != 9)
                    {
                        builder.AppendLine("*!*ERROR:  SuperSpeed Devices require bMaxPacketSize0 = 9 (512)\r\n");
                    }

                    break;
            }

            builder.AppendLine($"idVendor:                        0x{connectInfo.DeviceDescriptor.idVendor:X04}  = ");
#warning TODO
            //VendorString = GetVendorString(connectInfo.DeviceDescriptor.IdVendor);
            //if (VendorString != NULL)
            //{
            //    builder.AppendLine(" = %s\r\n",
            //        VendorString);
            //}

            builder.AppendLine($"idProduct:                       0x{connectInfo.DeviceDescriptor.idProduct:X04}");

            builder.AppendLine($"bcdDevice:                       0x{connectInfo.DeviceDescriptor.bcdDevice:X04}");

            builder.AppendLine(
                $"iManufacturer:                     0x{connectInfo.DeviceDescriptor.iManufacturer:X02}");
            builder.AppendLine($"                                     {device.Manufacturer}");

            builder.AppendLine($"iProduct:                          0x{connectInfo.DeviceDescriptor.iProduct:X02}");
            builder.AppendLine($"                                     {device.Product}");
            builder.AppendLine(
                $"iSerialNumber:                     0x{connectInfo.DeviceDescriptor.iSerialNumber:X02}");
            builder.AppendLine($"                                     {device.SerialNumber}");
            builder.AppendLine(
                $"bNumConfigurations:                0x{connectInfo.DeviceDescriptor.bNumConfigurations:X02}");

            if (connectInfo.DeviceDescriptor.bNumConfigurations != 1)
            {
                //@@TestCase A1.12
                //@@CAUTION
                //@@Descriptor Field - bNumConfigurations
                //@@Most host controllers do not handle more than one configuration
                builder.AppendLine("*!*CAUTION:    Most host controllers will only work with " +
                                   "one configuration per speed\r\n");
            }

#warning TODO
            if (connectInfo.NumberOfOpenPipes > 0)
            {
                builder.AppendLine("\r\n          ---===>Open Pipes<===---");
                //DisplayPipeInfo(connectInfo.NumberOfOpenPipes,
                //                connectInfo.PipeList);
            }
        }

        private void AppendNodeConnectionInfoExV2(StringBuilder builder,
            UsbIoControl.UsbNodeConnectionInformationExV2 connectionInformationExV2)
        {
            builder.AppendLine("Protocols Supported:");
            builder.AppendLine(
                $" USB 1.1:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb110).Display()}");
            builder.AppendLine(
                $" USB 2.0:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb200).Display()}");
            builder.AppendLine(
                $" USB 3.0:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb300).Display()}");
            builder.AppendLine();
        }

        private void AppendUsbPortConnectorProperties(StringBuilder builder,
            UsbIoControl.UsbPortConnectorProperties usbPortConnectorProperties)
        {
            builder.AppendLine(
                $"Is Port Connector Type C:         {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortConnectorIsTypeC).Display()}");
            builder.AppendLine(
                $"Is Port User Connectable:         {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortIsUserConnectable).Display()}");
            builder.AppendLine(
                $"Is Port Debug Capable:            {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortIsDebugCapable).Display()}");
            builder.AppendLine($"Companion Port Number:            {usbPortConnectorProperties.CompanionPortNumber}");
            builder.AppendLine(
                $"Companion Hub Symbolic Link Name: {usbPortConnectorProperties.CompanionHubSymbolicLinkName}");
        }

        private static void AppendEndpointDescriptor(StringBuilder builder, int index,
            UsbSpec.UsbEndpointDescriptor endpointDescriptor)
        {
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"ENDPOINT DESCRIPTOR {index}\n");
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"Endpoint Address\t\t\t: {endpointDescriptor.EndpointAddress:x}");
            builder.AppendLine($"Transfer Type\t\t\t: {endpointDescriptor.Attributes}");
            builder.AppendLine($"Max Packet Size\t\t\t: {endpointDescriptor.MaxPacketSize:x}");
            builder.AppendLine($"Update Interval\t\t\t: {endpointDescriptor.Interval:x}");
            builder.AppendLine($"Endpoint Descriptor Length\t\t: {endpointDescriptor.Length}");
        }

        private static void AppendHidDescriptor(StringBuilder builder, int index, HidApi.HidDescriptor hdiDescriptor)
        {
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"Human Device Interface DESCRIPTOR {index}");
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"HDI:{hdiDescriptor.BcdHid:x}");
            builder.AppendLine($"Country Code: {hdiDescriptor.Country:x}");
            builder.AppendLine($"Number of Descriptors: {hdiDescriptor.NumDescriptors:x}");
            builder.AppendLine(
                $"Descriptor Type: {hdiDescriptor.DescriptorType:x}");
            builder.AppendLine(
                $"Human Device Interface Descriptor Length: {hdiDescriptor.Length}");
            builder.AppendLine(
                $"Report Type: {hdiDescriptor.HidDesclist.ReportType:x}");
            builder.AppendLine(
                $"Report Length: {hdiDescriptor.HidDesclist.ReportLength:x}");
            builder.AppendLine("\n");
        }

        private static void AppendInterfaceDescriptor(StringBuilder builder, int index,
            UsbApi.UsbInterfaceDescriptor interfaceDescriptor)
        {
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"INTERFACE DESCRIPTOR {index}");
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine(
                $"Interface Number\t\t\t: {interfaceDescriptor.InterfaceNumber}");
            builder.AppendLine(
                $"Alternate Settings\t\t\t: {interfaceDescriptor.AlternateSetting}");
            builder.AppendLine(
                $"Number of Endpoints\t\t: {interfaceDescriptor.NumEndpoints}");
            builder.AppendLine($"Interface Class\t\t\t: {interfaceDescriptor.InterfaceClass:x}");
            builder.AppendLine($"Interface Sub Class\t\t\t: {interfaceDescriptor.InterfaceSubClass:x}");
            builder.AppendLine($"Interface Protocol\t\t\t: {interfaceDescriptor.InterfaceProtocol:x}");
            builder.AppendLine($"Index of the Interface\t\t: {interfaceDescriptor.Interface:x}");
            builder.AppendLine(
                $"Interface Descriptor Length\t\t: {interfaceDescriptor.Length}");
            builder.AppendLine("\n");
        }

        private static void AppendConfigurationDescriptor(StringBuilder builder, int index,
            UsbApi.UsbConfigurationDescriptor configurationDescriptor)
        {
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"CONFIGURATION DESCRIPTOR {index}");
            builder.AppendLine(
                "-----------------------------------------------------------------");
            builder.AppendLine($"Configuration Descriptor Length\t: {configurationDescriptor.Length}");
            builder.AppendLine($"Number of Interface Descriptors\t: {configurationDescriptor.NumInterface}");
            builder.AppendLine($"Configuration Value\t\t\t: {configurationDescriptor.ConfigurationsValue}");
            builder.AppendLine($"Index of the Configuration\t\t: {configurationDescriptor.IConfiguration}");
            builder.AppendLine($"Attributes\t\t\t\t: {configurationDescriptor.Attributes}");
            builder.AppendLine($"MaxPower\t\t\t: {configurationDescriptor.MaxPower}");
            builder.AppendLine("\n");
        }

        private static void AppendDeviceInformation(StringBuilder builder, Device device)
        {
            builder.AppendLine("       ---===>Device Information<===---");
            builder.AppendLine("English product name: \"TODO\"");
            builder.AppendLine($"\r\nConnectionStatus:                  {device.NodeConnectionInfo.ConnectionStatus}");
            builder.Append(
                $"Current Config Value:              0x{device.NodeConnectionInfo.CurrentConfigurationValue:X}");
            AppendDeviceSpeed(builder, device.NodeConnectionInfo.Speed, device.NodeConnectionInfoV2);
            builder.AppendLine($"Device Address:                    0x{device.NodeConnectionInfo.DeviceAddress:X}");
            builder.AppendLine($"Open Pipes:                          {device.NodeConnectionInfo.NumberOfOpenPipes}");
        }

        private static void AppendDeviceSpeed(StringBuilder builder, UsbSpec.UsbDeviceSpeed speed,
            UsbIoControl.UsbNodeConnectionInformationExV2 connectionInfoV2)
        {
            switch (speed)
            {
                case UsbSpec.UsbDeviceSpeed.UsbLowSpeed:
                    builder.AppendLine("  -> Device Bus Speed: Low");
                    break;
                case UsbSpec.UsbDeviceSpeed.UsbFullSpeed:

                    builder.Append("  -> Device Bus Speed: Full");

                    if (connectionInfoV2.Flags.IsSet(UsbIoControl.UsbNodeConnectionInformationExV2Flags
                        .DeviceIsOperatingAtSuperSpeedPlusOrHigher))
                        builder.AppendLine(" (is SuperSpeedPlus or higher capable)\r\n");
                    else if (connectionInfoV2.Flags.IsSet(UsbIoControl.UsbNodeConnectionInformationExV2Flags
                        .DeviceIsSuperSpeedCapableOrHigher))
                        builder.AppendLine(" (is SuperSpeed or higher capable)\r\n");
                    else
                        builder.AppendLine(" (is not SuperSpeed or higher capable)\r\n");

                    break;
                case UsbSpec.UsbDeviceSpeed.UsbHighSpeed:

                    builder.AppendLine("  -> Device Bus Speed: High");

                    if (connectionInfoV2.Flags.IsSet(UsbIoControl.UsbNodeConnectionInformationExV2Flags
                        .DeviceIsSuperSpeedPlusCapableOrHigher))
                        builder.AppendLine(" (is SuperSpeedPlus or higher capable)\r\n");
                    else if (connectionInfoV2.Flags.IsSet(UsbIoControl.UsbNodeConnectionInformationExV2Flags
                        .DeviceIsSuperSpeedCapableOrHigher))
                        builder.AppendLine(" (is SuperSpeed or higher capable)\r\n");
                    else
                        builder.AppendLine(" (is not SuperSpeed or higher capable)\r\n");

                    break;

                case UsbSpec.UsbDeviceSpeed.UsbSuperSpeed:

                    builder.AppendLine("  -> Device Bus Speed: Super");
                    builder.AppendLine(connectionInfoV2.Flags.IsSet(UsbIoControl.UsbNodeConnectionInformationExV2Flags
                        .DeviceIsOperatingAtSuperSpeedPlusOrHigher)
                        ? "SpeedPlus"
                        : "Speed");

                    break;

                default:
                    builder.AppendLine("  -> Device Bus Speed: Unknown\r\n");
                    break;
            }
        }

        private static void AppendUsbHub(StringBuilder builder, UsbHub hub)
        {
            builder.AppendLine(hub.DeviceDescription + "\r\n");
            builder.AppendLine($"Root Hub: {hub.DevicePath}");
            builder.AppendLine($"Hub Power:                    {(hub.IsBusPowered ? "Bus Power" : "Self Power")}");
            builder.AppendLine($"Number of Ports:              {hub.PortCount}");

            AppendHubCharacteristics(builder, hub.HubInformation.UsbHubDescriptor.HubCharacteristics);

            AppendHubCapabilities(builder, hub.UsbHubCapabilitiesEx.CapabilityFlags);
        }

        private static void AppendHubCapabilities(StringBuilder builder, UsbApi.UsbHubCapFlags capabilityFlags)
        {
            builder.AppendLine(
                $"High speed capable:           {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsHighSpeedCapable).Display()}");
            builder.AppendLine(
                $"High speed:                   {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsHighSpeed).Display()}");
            builder.AppendLine(
                $"Multiple transaction translations capable:                 {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsMultiTtCapable).Display()}");
            builder.AppendLine(
                $"Performs multiple transaction translations simultaneously: {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsMultiTt).Display()}");
            builder.AppendLine(
                $"Hub wakes when device is connected:                        {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsArmedWakeOnConnect).Display()}");
            builder.AppendLine(
                $"Hub is bus powered:           {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsBusPowered).Display()}");
            builder.AppendLine(
                $"Hub is root:                  {capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsRoot).Display()}");
        }

        private static void AppendHubCharacteristics(StringBuilder builder, short hubCharacteristics)
        {
            switch (hubCharacteristics & 0x0003)
            {
                case 0x0000:
                    builder.AppendLine("Power switching:              Ganged");
                    break;

                case 0x0001:
                    builder.AppendLine("Power switching:              Individual");
                    break;

                case 0x0002:
                case 0x0003:
                    builder.AppendLine("Power switching:              None");
                    break;
            }

            switch (hubCharacteristics & 0x0004)
            {
                case 0x0000:
                    builder.AppendLine("Compound device:              No");
                    break;

                case 0x0004:
                    builder.AppendLine("Compound device:              Yes");
                    break;
            }

            switch (hubCharacteristics & 0x0018)
            {
                case 0x0000:
                    builder.AppendLine("Over-current Protection:      Global");
                    break;

                case 0x0008:
                    builder.AppendLine("Over-current Protection:      Individual");
                    break;

                case 0x0010:
                case 0x0018:
                    builder.AppendLine("No Over-current Protection (Bus Power Only)");
                    break;
            }
        }

        private static void AppendUsbController(StringBuilder sb, UsbController controller)
        {
            sb.AppendLine($"{controller.DeviceDescription}\n\n");
            sb.AppendLine($"DriverKey: {controller.DriverKey}");
            sb.AppendLine($"VendorID: {controller.VendorId:X4}");
            sb.AppendLine($"DeviceID: {controller.DeviceId:X4}");
            sb.AppendLine($"SubSysID: {controller.SubSysId:X8}");
            sb.AppendLine($"Revision: {controller.Revision:X2}");

            sb.AppendLine("\r\nHost Controller Power State Mappings");
            sb.AppendLine(
                $"{"System State",-25}{"Host Controller",-25}{"Root Hub",-25}{"USB wakeup",-25}{"Powered",-25}");

            foreach (var usbPowerInfo in controller.PowerInfo)
                sb.AppendLine(
                    $"{usbPowerInfo.SystemState.Display(),-25}" +
                    $"{usbPowerInfo.HcDevicePowerState.Display(),-25}" +
                    $"{usbPowerInfo.RhDevicePowerState.Display(),-25}" +
                    $"{(usbPowerInfo.CanWakeup == 1).Display(),-25}" +
                    $"{(usbPowerInfo.IsPowered == 1).Display(),-25}");

            sb.AppendLine($"Last Sleep State\t{controller.PowerInfo.Last().LastSystemSleepState.Display()}");
        }

        #region toolstrip events

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }


        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_UsbTreeView.SelectedNode is UsbTreeNode usbNode)
            {
                var port = usbNode.Device as UsbDevice;
                port?.Enable(port.DeviceDescriptor.idVendor, port.DeviceDescriptor.idProduct);
            }
        }

        private void disableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var usbNode = m_UsbTreeView.SelectedNode as UsbTreeNode;
            if (usbNode?.Device is UsbDevice port)
                port.Disable(port.DeviceDescriptor.idVendor, port.DeviceDescriptor.idProduct);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var usbNode = m_UsbTreeView.SelectedNode as UsbTreeNode;
            if (usbNode?.Device is UsbDevice port)
                port.OpenDevice();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReScanUsbBus();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                ReScanUsbBus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #endregion
    }
}
#region references

using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NativeUsbLib;
using NativeUsbLib.WinApis;
using UsbViewer.Extensions;

#endregion

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

                if ((usbNode.Type == DeviceTyp.Hub || usbNode.Type == DeviceTyp.RootHub) && usbNode.Device is UsbHub hub)
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

                        if (device.DeviceDescription != null)
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

        private void AppendNodeConnectionInfoExV2(StringBuilder sb, UsbIoControl.UsbNodeConnectionInformationExV2 connectionInformationExV2)
        {
            sb.AppendLine("Protocols Supported:");
            sb.AppendLine($" USB 1.1:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb110).Display()}");
            sb.AppendLine($" USB 2.0:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb200).Display()}");
            sb.AppendLine($" USB 3.0:                         {connectionInformationExV2.SupportedUsbProtocols.IsSet(UsbIoControl.UsbProtocols.Usb300).Display()}");
        }

        private void AppendUsbPortConnectorProperties(StringBuilder sb, UsbIoControl.UsbPortConnectorProperties usbPortConnectorProperties)
        {
            sb.AppendLine($"Is Port Connector Type C:         {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortConnectorIsTypeC).Display()}");
            sb.AppendLine($"Is Port User Connectable:         {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortIsUserConnectable).Display()}");
            sb.AppendLine($"Is Port Debug Capable:            {usbPortConnectorProperties.Properties.IsSet(UsbIoControl.UsbPortProperties.PortIsDebugCapable).Display()}");
            sb.AppendLine($"Companion Port Number:            {usbPortConnectorProperties.CompanionPortNumber}");
            sb.AppendLine($"Companion Hub Symbolic Link Name: {usbPortConnectorProperties.CompanionHubSymbolicLinkName}");
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
                $"-----------------------------------------------------------------");
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

        private static void AppendDeviceDescriptor(StringBuilder builder, Device device)
        {
            builder.AppendLine($"{device.DeviceDescription}\r\n");

            builder.AppendLine(
                $"USB Version\t\t\t: {device.DeviceDescriptor.bcdUSB.ToString("x").Replace("0", "")}");
            builder.AppendLine($"DeviceClass\t\t\t: {device.DeviceDescriptor.bDeviceClass:x}");
            builder.AppendLine($"DeviceSubClass\t\t\t: {device.DeviceDescriptor.DeviceSubClass:x}");
            builder.AppendLine($"DeviceProtocol\t\t\t: {device.DeviceDescriptor.DeviceProtocol:x}");
            builder.AppendLine($"MaxPacketSize\t\t\t: {device.DeviceDescriptor.MaxPacketSize0:x}");
            builder.AppendLine($"Vendor ID\t\t\t: {device.DeviceDescriptor.IdVendor:x}");
            builder.AppendLine($"Product ID\t\t\t: {device.DeviceDescriptor.IdProduct:x}");
            builder.AppendLine(
                $"bcdDevice\t\t\t: {device.DeviceDescriptor.bcdDevice:x}");
            builder.AppendLine(
                $"Device String Index of the Manufacturer\t: {device.DeviceDescriptor.IManufacturer:x}");
            builder.AppendLine($"Manufacturer\t\t\t: {device.Manufacturer}");
            builder.AppendLine($"Device String Index of the Product\t: {device.DeviceDescriptor.IProduct:x}");
            builder.AppendLine($"Product\t\t\t\t: {device.Product}");
            builder.AppendLine($"Device String Index of the Serial Number: {device.DeviceDescriptor.ISerialNumber:x}");
            builder.AppendLine($"Serial Number\t\t\t: {device.SerialNumber}");
            builder.AppendLine($"Number of available configurations\t: {device.DeviceDescriptor.NumConfigurations:x}");
            builder.AppendLine(
                $"Descriptor Type\t\t\t: {device.DeviceDescriptor.DescriptorType}");
            builder.AppendLine(
                $"Device Descriptor Length\t\t: {device.DeviceDescriptor.Length}");
            builder.AppendLine("\n");
            builder.AppendLine($"ConnectionStatus\t\t\t: {device.Status}");
            builder.AppendLine("Curent Config Value\t\t\t:");
            builder.AppendLine($"Device Bus Speed\t\t\t: {device.Speed}");
            builder.AppendLine($"Device Address\t\t\t: ");
            builder.AppendLine($"Open Pipes\t\t\t: ");
            builder.AppendLine($"DriverKeyName\t\t\t: {device.DriverKey}");
            builder.AppendLine($"AdapterNumber\t\t\t: {device.AdapterNumber}");
            builder.AppendLine($"Instance ID\t\t\t: {device.InstanceId}");
            //sb.AppendLine("SerialNumber\t\t\t: " + port.Device.SerialNumber + "\n");
            builder.AppendLine("\n");
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
                $"Performs multiple transaction translations simultaneously: {(capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsMultiTt)).Display()}");
            builder.AppendLine(
                $"Hub wakes when device is connected:                        {(capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsArmedWakeOnConnect)).Display()}");
            builder.AppendLine(
                $"Hub is bus powered:           {(capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsBusPowered)).Display()}");
            builder.AppendLine(
                $"Hub is root:                  {(capabilityFlags.IsSet(UsbApi.UsbHubCapFlags.HubIsRoot)).Display()}");
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
            sb.AppendLine($"{"System State", -25}{"Host Controller",-25}{"Root Hub",-25}{"USB wakeup",-25}{"Powered",-25}");

            foreach (var usbPowerInfo in controller.PowerInfo)
            {
                sb.AppendLine(
                    $"{usbPowerInfo.SystemState.Display(),-25}" +
                    $"{usbPowerInfo.HcDevicePowerState.Display(),-25}" +
                    $"{usbPowerInfo.RhDevicePowerState.Display(),-25}" +
                    $"{(usbPowerInfo.CanWakeup == 1).Display(),-25}" +
                    $"{(usbPowerInfo.IsPowered == 1).Display(),-25}");
            }

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
                port?.Enable(port.DeviceDescriptor.IdVendor, port.DeviceDescriptor.IdProduct);
            }
        }

        private void disableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var usbNode = m_UsbTreeView.SelectedNode as UsbTreeNode;
            if (usbNode?.Device is UsbDevice port)
                port.Disable(port.DeviceDescriptor.IdVendor, port.DeviceDescriptor.IdProduct);
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
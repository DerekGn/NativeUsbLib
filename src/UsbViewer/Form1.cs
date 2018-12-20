#region references

using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NativeUsbLib;
using NativeUsbLib.WinApis;

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

                foreach (var device in hub.Devices) ShowDevice(usbNode, device);
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
            StringBuilder sb = new StringBuilder();

            if (e.Node is UsbTreeNode usbNode)
            {
                if (usbNode.Type == DeviceTyp.Controller)
                {
                    var controller = usbNode.Device as UsbController;

                    AppendUsbController(sb, controller);
                }

                if (usbNode.Type == DeviceTyp.Hub || usbNode.Type == DeviceTyp.RootHub)
                {
                    if(usbNode.Device is UsbHub hub)
                        AppendUsbHub(sb, hub);
                }

                if (usbNode.Type == DeviceTyp.Device || usbNode.Type == DeviceTyp.Hub)
                {
                    var device = usbNode.Device;
                    if (device.IsConnected)
                    {
                        var index = 1;

                        if (device.DeviceDescription != null)
                        {
                            AppendDeviceDescriptor(sb, device);
                        }

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

        private static void AppendEndpointDescriptor(StringBuilder sb, int index, UsbApi.UsbEndpointDescriptor endpointDescriptor)
        {
            sb.AppendLine(
                "-----------------------------------------------------------------");
            sb.AppendLine($"ENDPOINT DESCRIPTOR {index}\n");
            sb.AppendLine(
                "-----------------------------------------------------------------");
            sb.AppendLine($"Endpoint Address\t\t\t: {endpointDescriptor.EndpointAddress:x}");
            sb.AppendLine($"Transfer Type\t\t\t: {endpointDescriptor.Attributes}");
            sb.AppendLine($"Max Packet Size\t\t\t: {endpointDescriptor.MaxPacketSize:x}");
            sb.AppendLine($"Update Interval\t\t\t: {endpointDescriptor.Interval:x}");
            sb.AppendLine($"Endpoint Descriptor Length\t\t: {endpointDescriptor.Length}");
        }

        private static void AppendHidDescriptor(StringBuilder sb, int index, HidApi.HidDescriptor hdiDescriptor)
        {
            sb.AppendLine(
                "-----------------------------------------------------------------\n");
            sb.AppendLine("Human Device Interface DESCRIPTOR " + index + "\n");
            sb.AppendLine(
                "-----------------------------------------------------------------\n");
            sb.AppendLine("HDI:" + hdiDescriptor.BcdHid.ToString("x") + "\n");
            sb.AppendLine("Country Code: " + hdiDescriptor.Country.ToString("x") + "\n");
            sb.AppendLine("Number of Descriptors: " +
                          hdiDescriptor.NumDescriptors.ToString("x") + "\n");
            sb.AppendLine(
                "Descriptor Type: " + hdiDescriptor.DescriptorType.ToString("x") + "\n");
            sb.AppendLine(
                "Human Device Interface Descriptor Length: " + hdiDescriptor.Length + "\n");
            sb.AppendLine(
                "Report Type: " + hdiDescriptor.HidDesclist.ReportType.ToString("x") + "\n");
            sb.AppendLine(
                "Report Length: " + hdiDescriptor.HidDesclist.ReportLength.ToString("x") + "\n");
            sb.AppendLine("\n");
        }

        private static void AppendInterfaceDescriptor(StringBuilder sb, int index, UsbApi.UsbInterfaceDescriptor interfaceDescriptor)
        {
            sb.AppendLine(
                $"-----------------------------------------------------------------\n");
            sb.AppendLine("INTERFACE DESCRIPTOR " + index + "\n");
            sb.AppendLine(
                "-----------------------------------------------------------------\n");
            sb.AppendLine(
                "Interface Number\t\t\t: " + interfaceDescriptor.InterfaceNumber + "\n");
            sb.AppendLine(
                "Alternate Settings\t\t\t: " + interfaceDescriptor.AlternateSetting + "\n");
            sb.AppendLine(
                "Number of Endpoints\t\t: " + interfaceDescriptor.NumEndpoints + "\n");
            sb.AppendLine("Interface Class\t\t\t: " +
                          interfaceDescriptor.InterfaceClass.ToString("x") + "\n");
            sb.AppendLine("Interface Sub Class\t\t\t: " +
                          interfaceDescriptor.InterfaceSubClass.ToString("x") + "\n");
            sb.AppendLine("Interface Protocol\t\t\t: " +
                          interfaceDescriptor.InterfaceProtocol.ToString("x") + "\n");
            sb.AppendLine("Index of the Interface\t\t: " +
                          interfaceDescriptor.Interface.ToString("x") + "\n");
            sb.AppendLine(
                "Interface Descriptor Length\t\t: " + interfaceDescriptor.Length + "\n");
            sb.AppendLine("\n");
        }

        private static void AppendConfigurationDescriptor(StringBuilder sb, int index,
            UsbApi.UsbConfigurationDescriptor configurationDescriptor)
        {
            sb.AppendLine(
                "-----------------------------------------------------------------\n");
            sb.AppendLine("CONFIGURATION DESCRIPTOR " + index + "\n");
            sb.AppendLine(
                "-----------------------------------------------------------------\n");
            sb.AppendLine("Configuration Descriptor Length\t: " +
                          configurationDescriptor.Length + "\n");
            sb.AppendLine("Number of Interface Descriptors\t: " +
                          configurationDescriptor.NumInterface + "\n");
            sb.AppendLine("Configuration Value\t\t\t: " +
                          configurationDescriptor.ConfigurationsValue + "\n");
            sb.AppendLine("Index of the Configuration\t\t: " +
                          configurationDescriptor.IConfiguration + "\n");
            sb.AppendLine(
                "Attributes\t\t\t\t: " + configurationDescriptor.Attributes + "\n");
            sb.AppendLine("MaxPower\t\t\t: " + configurationDescriptor.MaxPower + "\n");
            sb.AppendLine("\n");
        }

        private static void AppendDeviceDescriptor(StringBuilder sb, Device device)
        {
            sb.AppendLine("-----------------------------------------------------------------");
            sb.AppendLine("DEVICE DESCRIPTOR");
            sb.AppendLine("-----------------------------------------------------------------");
            sb.AppendLine(
                $"USB Version\t\t\t: {device.DeviceDescriptor.bcdUSB.ToString("x").Replace("0", "")}");
            sb.AppendLine($"DeviceClass\t\t\t: {device.DeviceDescriptor.bDeviceClass:x}");
            sb.AppendLine($"DeviceSubClass\t\t\t: {device.DeviceDescriptor.DeviceSubClass:x}");
            sb.AppendLine($"DeviceProtocol\t\t\t: {device.DeviceDescriptor.DeviceProtocol:x}");
            sb.AppendLine($"MaxPacketSize\t\t\t: {device.DeviceDescriptor.MaxPacketSize0:x}");
            sb.AppendLine($"Vendor ID\t\t\t: {device.DeviceDescriptor.IdVendor:x}");
            sb.AppendLine($"Product ID\t\t\t: {device.DeviceDescriptor.IdProduct:x}");
            sb.AppendLine(
                $"bcdDevice\t\t\t: {device.DeviceDescriptor.bcdDevice:x}\n");
            sb.AppendLine(
                $"Device String Index of the Manufacturer\t: {device.DeviceDescriptor.IManufacturer:x}\n");
            sb.AppendLine("Manufacturer\t\t\t: " + device.Manufacturer + "\n");
            sb.AppendLine("Device String Index of the Product\t: " +
                          device.DeviceDescriptor.IProduct.ToString("x") + "\n");
            sb.AppendLine("Product\t\t\t\t: " + device.Product + "\n");
            sb.AppendLine("Device String Index of the Serial Number: " +
                          device.DeviceDescriptor.ISerialNumber.ToString("x") + "\n");
            sb.AppendLine("Serial Number\t\t\t: " + device.SerialNumber + "\n");
            sb.AppendLine("Number of available configurations\t: " +
                          device.DeviceDescriptor.NumConfigurations.ToString("x") + "\n");
            sb.AppendLine(
                "Descriptor Type\t\t\t: " + device.DeviceDescriptor.DescriptorType + "\n");
            sb.AppendLine(
                "Device Descriptor Length\t\t: " + device.DeviceDescriptor.Length + "\n");
            sb.AppendLine("\n");
            sb.AppendLine("ConnectionStatus\t\t\t: " + device.Status + "\n");
            sb.AppendLine("Curent Config Value\t\t\t: " + "\n");
            sb.AppendLine("Device Bus Speed\t\t\t: " + device.Speed + "\n");
            sb.AppendLine("Device Address\t\t\t: " + "\n");
            sb.AppendLine("Open Pipes}\t\t\t: " + "\n");
            sb.AppendLine("DriverKeyName\t\t\t: " + device.DriverKey + "\n");
            sb.AppendLine("AdapterNumber\t\t\t: " + device.AdapterNumber + "\n");
            sb.AppendLine("Instance ID\t\t\t: " + device.InstanceId + "\n");
            //sb.AppendLine("SerialNumber\t\t\t: " + port.Device.SerialNumber + "\n");
            sb.AppendLine("\n");
        }

        private static void AppendUsbHub(StringBuilder sb, UsbHub hub)
        {
            sb.AppendLine(hub.DeviceDescription);

            sb.AppendLine(
                $"Descriptor Type\t\t\t: {hub.NodeInformation.HubInformation.HubDescriptor.DescriptorType}");
            sb.AppendLine(
                $"Descriptor length (Byte)\t\t: {hub.NodeInformation.HubInformation.HubDescriptor.DescriptorLength}");
            sb.AppendLine($"HubName\t\t\t: {hub.DeviceDescription}");
            sb.AppendLine($"HubDevicePath\t\t\t: {hub.DevicePath}");

            sb.AppendLine($"Hub Power:                    {(hub.IsBusPowered ? "Bus Power" : "Self Power")}");

            sb.AppendLine($"Number of Downstream Ports\t\t: {hub.PortCount}");
            sb.AppendLine(
                $"Settling time of the power supply (ms)\t: {hub.NodeInformation.HubInformation.HubDescriptor.PowerOnToPowerGood * 2}");
            sb.AppendLine(
                $"Max power (mA)\t\t\t: {hub.NodeInformation.HubInformation.HubDescriptor.HubControlCurrent}");
            sb.AppendLine(
                $"HubCharacteristics\t\t\t: {Convert.ToString(hub.NodeInformation.HubInformation.HubDescriptor.HubCharacteristics, 2)}");
            sb.AppendLine($"Is Root Hub\t\t\t:{hub.IsRootHub}");
        }

        private static void AppendUsbController(StringBuilder sb, UsbController controller)
        {
            sb.AppendLine($"{controller.DeviceDescription}\n\n");
            sb.AppendLine($"DriverKey: {controller.DriverKey}");
            sb.AppendLine($"VendorID: {controller.VendorId:X4}");
            sb.AppendLine($"DeviceID: {controller.DeviceId:X4}");
            sb.AppendLine($"SubSysID: {controller.SubSysID:X8}");
            sb.AppendLine($"Revision: {controller.Revision:X2}");

            sb.AppendLine($"DevicePath\t\t\t: {controller.DevicePath}\n");
            
            sb.AppendLine($"Index\t\t\t\t: {controller.AdapterNumber}\n");
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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
        private const int PcProtocolUndefined = 0;

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
                        sb.AppendLine($"[Port{device.AdapterNumber}]  :  {device.DeviceDescription}\r\n");

                        AppendUsbPortConnectorProperties(sb, device.UsbPortConnectorProperties);

                        AppendNodeConnectionInfoExV2(sb, device.NodeConnectionInfoV2);

                        AppendDeviceInformation(sb, device);

                        AppendDeviceDescriptor(sb, device);

                        sb.AppendLine("       ---===>Full Configuration Descriptor<===---");

                        AppendConfigurationDescriptors(sb, device);

                        AppendInterfaceDescriptors(sb, device);

                        AppendHidDescriptors(sb, device);

                        AppendEndpointDescriptors(sb, device);
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

        private void AppendEndpointDescriptors(StringBuilder builder, Device device)
        {
            foreach (var endpointDescriptor in device.EndpointDescriptors)
            {
                //PUSB_HIGH_SPEED_MAXPACKET hsMaxPacket;

                builder.AppendLine("\r\n          ===>Endpoint Descriptor<===");
                //@@DisplayEndpointDescriptor - Endpoint Descriptor
                //length checked in DisplayConfigDesc()

                builder.AppendLine($"bLength:                           0x{endpointDescriptor.bLength:X02}");
                builder.AppendLine($"bDescriptorType:                   0x{(int)endpointDescriptor.bDescriptorType:X02}");
                builder.Append($"bEndpointAddress:                  0x{endpointDescriptor.bEndpointAddress:X02}");

                //if (endpointDescriptor.IsEndpointOut())
                //{
                //    builder.AppendLine($"  -> Direction: OUT - EndpointID: {endpointDescriptor.GetEndpointId():D}");
                //}
                //else if (endpointDescriptor.IsEndpointIn())
                //{
                //    builder.AppendLine($"  -> Direction: IN - EndpointID: {endpointDescriptor.GetEndpointId():D}");
                //}
                //else
                //{
                //    //@@TestCase A6.1
                //    //@@ERROR
                //    //@@Descriptor Field - bEndpointAddress
                //    //@@An invalid endpoint addressl has been defined
                //    builder.AppendLine("\r\n*!*ERROR:  This appears to be an invalid bEndpointAddress");
                //}

                builder.Append($"bmAttributes:                      0x{endpointDescriptor.bmAttributes:X02}");
                builder.Append("  -> ");

                //switch (endpointDescriptor.GetEndpointType())
                //{
                //    case UsbSpec.UsbEndpointType.Control:
                //        builder.AppendLine("Control Transfer Type\r\n");
                //        if ((endpointDescriptor.bmAttributes & UsbSpec.UsbEndpointTypeControlReservedMask) > 0)
                //        {
                //            builder.AppendLine("\r\n*!*ERROR:     Bits 7..2 are reserved and must be set to 0\r\n");
                //        }
                //        break;

                //    case UsbSpec.UsbEndpointType.Isochronous:
                //        builder.AppendLine("Isochronous Transfer Type, Synchronization Type = ");

                //        switch (endpointDescriptor.GetSynchronization())
                //        {
                //            case UsbSpec.EndpointSynchronization.IsochronousSynchronizationNoSynchronization:
                //                builder.AppendLine("No Synchronization");
                //                break;

                //            case UsbSpec.EndpointSynchronization.IsochronousSynchronizationAsynchronous:
                //                builder.AppendLine("Asynchronous");
                //                break;

                //            case UsbSpec.EndpointSynchronization.IsochronousSynchronizationAdaptive:
                //                builder.AppendLine("Adaptive");
                //                break;

                //            case UsbSpec.EndpointSynchronization.IsochronousSynchronizationSynchronous:
                //                builder.AppendLine("Synchronous");
                //                break;
                //        }
                //        builder.AppendLine(", Usage Type = ");

                //        switch (endpointDescriptor.GetIsochronousUsage())
                //        {
                //            case UsbSpec.EndpointIsochronousUsage.IsochronousUsageDataEndoint:
                //                builder.AppendLine("Data Endpoint");
                //                break;

                //            case UsbSpec.EndpointIsochronousUsage.IsochronousUsageFeedbackEndpoint:
                //                builder.AppendLine("Feedback Endpoint");
                //                break;

                //            case UsbSpec.EndpointIsochronousUsage.IsochronousUsageImplicitFeedbackDataEndpoint:
                //                builder.AppendLine("Implicit Feedback Data Endpoint");
                //                break;

                //            case UsbSpec.EndpointIsochronousUsage.IsochronousUsageReserved:
                //                //@@TestCase A6.2
                //                //@@ERROR
                //                //@@Descriptor Field - bmAttributes
                //                //@@A reserved bit has a value
                //                builder.AppendLine("\r\n*!*ERROR:     This value is Reserved");
                                
                //                break;
                //        }
                //        if ((endpointDescriptor.bmAttributes & UsbSpec.UsbEndpointTypeIsochronousReservedMask) > 0)
                //        {
                //            builder.AppendLine("\r\n*!*ERROR:     Bits 7..6 are reserved and must be set to 0");
                //        }
                //        break;

                //    case UsbSpec.UsbEndpointType.Bulk:
                //        builder.AppendLine("Bulk Transfer Type");
                //        if ((endpointDescriptor.bmAttributes & UsbSpec.UsbEndpointTypeBulkReservedMask) > 0)
                //        {
                //            builder.AppendLine("\r\n*!*ERROR:     Bits 7..2 are reserved and must be set to 0");
                            
                //        }
                //        break;

                //    case UsbSpec.UsbEndpointType.Interrupt:

                //        //if (gDeviceSpeed != UsbSuperSpeed)
                //        //{
                //        //    builder.AppendLine("Interrupt Transfer Type\r\n");
                //        //    if (endpointDescriptor.bmAttributes & USB_20_ENDPOINT_TYPE_INTERRUPT_RESERVED_MASK)
                //        //    {
                //        //        builder.AppendLine("\r\n*!*ERROR:     Bits 7..2 are reserved and must be set to 0\r\n");
                //        //    }
                //        //}
                //        //else
                //        //{
                //        //    builder.AppendLine("Interrupt Transfer Type, Usage Type = ");

                //        //    switch (USB_30_ENDPOINT_TYPE_INTERRUPT_USAGE(endpointDescriptor.bmAttributes))
                //        //    {
                //        //        case USB_30_ENDPOINT_TYPE_INTERRUPT_USAGE_PERIODIC:
                //        //            builder.AppendLine("Periodic\r\n");
                //        //            break;

                //        //        case USB_30_ENDPOINT_TYPE_INTERRUPT_USAGE_NOTIFICATION:
                //        //            builder.AppendLine("Notification\r\n");
                //        //            break;

                //        //        case USB_30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED10:
                //        //        case USB_30_ENDPOINT_TYPE_INTERRUPT_USAGE_RESERVED11:
                //        //            builder.AppendLine("\r\n*!*ERROR:     This value is Reserved\r\n");
                //        //            break;
                //        //    }

                //        //    if (endpointDescriptor.bmAttributes & USB_30_ENDPOINT_TYPE_INTERRUPT_RESERVED_MASK)
                //        //    {
                //        //        builder.AppendLine("\r\n*!*ERROR:     Bits 7..6 and 3..2 are reserved and must be set to 0\r\n");
                //        //    }

                //        //    if (EpCompDescAvail)
                //        //    {
                //        //        if (EpCompDesc == NULL)
                //        //        {
                //        //            builder.AppendLine("\r\n*!*ERROR:     Endpoint Companion Descriptor missing\r\n");
                //        //        }
                //        //        else if (EpCompDesc->bmAttributes.Isochronous.SspCompanion == 1 &&
                //        //            SspIsochEpCompDesc == NULL)
                //        //        {
                //        //            builder.AppendLine("\r\n*!*ERROR:     SuperSpeedPlus Isoch Endpoint Companion Descriptor missing\r\n");
                //        //        }
                //        //    }
                //        //}
                //        break;
                //}


                //@@TestCase A6.3
                //@@Priority 1
                //@@Descriptor Field - bInterfaceNumber
                //@@Question - Should we test to verify bInterfaceNumber is valid?
                builder.AppendLine($"wMaxPacketSize:                  0x{endpointDescriptor.wMaxPacketSize:X04}");

                //switch (gDeviceSpeed)
                //{
                //    case UsbSuperSpeed:
                //        switch (epType)
                //        {
                //            case USB_ENDPOINT_TYPE_BULK:
                //                if (endpointDescriptor.wMaxPacketSize != USB_ENDPOINT_SUPERSPEED_BULK_MAX_PACKET_SIZE)
                //                {
                //                    builder.AppendLine("\r\n*!*ERROR:     SuperSpeed Bulk endpoints must be %d bytes\r\n",
                //                        USB_ENDPOINT_SUPERSPEED_BULK_MAX_PACKET_SIZE);
                //                }
                //                else
                //                {
                //                    builder.AppendLine("\r\n");
                //                }
                //                break;

                //            case USB_ENDPOINT_TYPE_CONTROL:
                //                if (endpointDescriptor.wMaxPacketSize != USB_ENDPOINT_SUPERSPEED_CONTROL_MAX_PACKET_SIZE)
                //                {
                //                    builder.AppendLine("\r\n*!*ERROR:     SuperSpeed Control endpoints must be %d bytes\r\n",
                //                        USB_ENDPOINT_SUPERSPEED_CONTROL_MAX_PACKET_SIZE);
                //                }
                //                else
                //                {
                //                    builder.AppendLine("\r\n");
                //                }
                //                break;

                //            case USB_ENDPOINT_TYPE_ISOCHRONOUS:

                //                if (EpCompDesc != NULL)
                //                {
                //                    if (EpCompDesc->bMaxBurst > 0)
                //                    {
                //                        if (endpointDescriptor.wMaxPacketSize != USB_ENDPOINT_SUPERSPEED_ISO_MAX_PACKET_SIZE)
                //                        {
                //                            builder.AppendLine("\r\n*!*ERROR:     SuperSpeed isochronous endpoints must have wMaxPacketSize value of %d bytes\r\n",
                //                                USB_ENDPOINT_SUPERSPEED_ISO_MAX_PACKET_SIZE);
                //                            builder.AppendLine("                  when the SuperSpeed endpoint companion descriptor bMaxBurst value is greater than 0\r\n");
                //                        }
                //                        else
                //                        {
                //                            builder.AppendLine("\r\n");
                //                        }
                //                    }
                //                    else if (endpointDescriptor.wMaxPacketSize > USB_ENDPOINT_SUPERSPEED_ISO_MAX_PACKET_SIZE)
                //                    {
                //                        builder.AppendLine("\r\n*!*ERROR:     Invalid SuperSpeed isochronous maximum packet size\r\n");
                //                    }
                //                    else
                //                    {
                //                        builder.AppendLine("\r\n");
                //                    }
                //                }
                //                else
                //                {
                //                    builder.AppendLine("\r\n");
                //                }
                //                break;

                //            case USB_ENDPOINT_TYPE_INTERRUPT:

                //                if (EpCompDesc != NULL)
                //                {
                //                    if (EpCompDesc->bMaxBurst > 0)
                //                    {
                //                        if (endpointDescriptor.wMaxPacketSize != USB_ENDPOINT_SUPERSPEED_INTERRUPT_MAX_PACKET_SIZE)
                //                        {
                //                            builder.AppendLine("\r\n*!*ERROR:     SuperSpeed interrupt endpoints must have wMaxPacketSize value of %d bytes\r\n",
                //                                USB_ENDPOINT_SUPERSPEED_INTERRUPT_MAX_PACKET_SIZE);
                //                            builder.AppendLine("                  when the SuperSpeed endpoint companion descriptor bMaxBurst value is greater than 0\r\n");
                //                        }
                //                        else
                //                        {
                //                            builder.AppendLine("\r\n");
                //                        }
                //                    }
                //                    else if (endpointDescriptor.wMaxPacketSize > USB_ENDPOINT_SUPERSPEED_INTERRUPT_MAX_PACKET_SIZE)
                //                    {
                //                        builder.AppendLine("\r\n*!*ERROR:     Invalid SuperSpeed interrupt maximum packet size\r\n");
                //                    }
                //                    else
                //                    {
                //                        builder.AppendLine("\r\n");
                //                    }
                //                }
                //                else
                //                {
                //                    builder.AppendLine("\r\n");
                //                }
                //                break;
                //        }
                //        break;

                //    case UsbHighSpeed:
                //        hsMaxPacket = (PUSB_HIGH_SPEED_MAXPACKET) & endpointDescriptor.wMaxPacketSize;

                //        switch (epType)
                //        {
                //            case USB_ENDPOINT_TYPE_ISOCHRONOUS:
                //            case USB_ENDPOINT_TYPE_INTERRUPT:
                //                switch (hsMaxPacket->HSmux)
                //                {
                //                    case 0:
                //                        if ((hsMaxPacket->MaxPacket < 1) || (hsMaxPacket->MaxPacket > 1024))
                //                        {
                //                            builder.AppendLine("*!*ERROR:  Invalid maximum packet size, should be between 1 and 1024\r\n");
                //                        }
                //                        break;

                //                    case 1:
                //                        if ((hsMaxPacket->MaxPacket < 513) || (hsMaxPacket->MaxPacket > 1024))
                //                        {
                //                            builder.AppendLine("*!*ERROR:  Invalid maximum packet size, should be between 513 and 1024\r\n");
                //                        }
                //                        break;

                //                    case 2:
                //                        if ((hsMaxPacket->MaxPacket < 683) || (hsMaxPacket->MaxPacket > 1024))
                //                        {
                //                            builder.AppendLine("*!*ERROR:  Invalid maximum packet size, should be between 683 and 1024\r\n");
                //                        }
                //                        break;

                //                    case 3:
                //                        builder.AppendLine("*!*ERROR:  Bits 12-11 set to Reserved value in wMaxPacketSize\r\n");
                //                        break;
                //                }

                //                builder.AppendLine(" = %d transactions per microframe, 0x%02X max bytes\r\n", hsMaxPacket->HSmux + 1, hsMaxPacket->MaxPacket);
                //                break;

                //            case USB_ENDPOINT_TYPE_BULK:
                //            case USB_ENDPOINT_TYPE_CONTROL:
                //                builder.AppendLine(" = 0x%02X max bytes\r\n", hsMaxPacket->MaxPacket);
                //                break;
                //        }
                //        break;

                //    case UsbFullSpeed:
                //        // full speed
                //        builder.AppendLine(" = 0x%02X bytes\r\n",
                //            endpointDescriptor.wMaxPacketSize & 0x7FF);
                //        break;
                //    default:
                //        // low or invalid speed
                //        if (InterfaceClass == USB_DEVICE_CLASS_VIDEO)
                //        {
                //            builder.AppendLine(" = Invalid bus speed for USB Video Class\r\n");
                //        }
                //        else
                //        {
                //            builder.AppendLine("\r\n");
                //        }
                //        break;
                //}


                if ((endpointDescriptor.wMaxPacketSize & 0xE000) > 0)
                {
                    //@@TestCase A6.4
                    //@@Priority 1
                    //@@OTG Descriptor Field - wMaxPacketSize
                    //@@Attribute bits D7-2 reserved (reset to 0)
                    builder.AppendLine("*!*ERROR:  wMaxPacketSize bits 15-13 should be 0");
                }

                if (endpointDescriptor.bLength == Marshal.SizeOf<UsbSpec.UsbEndpointDescriptor>())
                {
                    //@@TestCase A6.5
                    //@@Priority 1
                    //@@Descriptor Field - bInterfaceNumber
                    //@@Question - Should we test to verify bInterfaceNumber is valid?
                    builder.AppendLine($"bInterval:                         0x{endpointDescriptor.bInterval:X02}");
                }
                else
                {
                    //PUSB_ENDPOINT_DESCRIPTOR2 endpointDesc2;

                    //endpointDesc2 = (PUSB_ENDPOINT_DESCRIPTOR2)EndpointDesc;

                    //builder.AppendLine("wInterval:                       0x%04X\r\n",
                    //    endpointDesc2->wInterval);

                    //builder.AppendLine("bSyncAddress:                      0x%02X\r\n",
                    //    endpointDesc2->bSyncAddress);
                }

                //if (EpCompDesc != NULL)
                //{
                //    DisplayEndointCompanionDescriptor(EpCompDesc, SspIsochEpCompDesc, epType);
                //}
                //if (SspIsochEpCompDesc != NULL)
                //{
                //    DisplaySuperSpeedPlusIsochEndpointCompanionDescriptor(SspIsochEpCompDesc);
                //}
            }
        }

        private void AppendHidDescriptors(StringBuilder builder, Device device)
        {
            foreach (var hidDescriptor in device.HidDescriptors)
            {
                builder.AppendLine("\r\n          ===>HID Descriptor<===");
                builder.AppendLine($"bLength:                           0x{hidDescriptor.bLength:X02}");
                builder.AppendLine($"bDescriptorType:                   0x{(int)hidDescriptor.bDescriptorType:X02} -> {hidDescriptor.bDescriptorType}");
                builder.AppendLine($"bcdHID:                            0x{(int)hidDescriptor.BcdHid:X04}");
                builder.AppendLine($"bCountryCode:                      0x{(int)hidDescriptor.bCountryCode:X02}");
                builder.AppendLine($"bNumDescriptors:                   0x{(int)hidDescriptor.bNumDescriptors:X02}");

#warning TODO
                //foreach (var optionalDescriptor in hidDescriptor.OptionalDescriptors)
                //{
                //    if (optionalDescriptor.bDescriptorType == 0x22)
                //    {
                //        builder.AppendLine($"bDescriptorType:                   0x{optionalDescriptor.bDescriptorType:X02} (Report Descriptor)");
                //    }
                //    else
                //    {
                //        builder.AppendLine($"bDescriptorType:                   0x{optionalDescriptor.bDescriptorType:X02}");
                //    }

                //    builder.AppendLine($"wDescriptorLength:               0x{optionalDescriptor.wDescriptorLength:X04}");
                //}
            }
        }

        private void AppendInterfaceDescriptors(StringBuilder builder, Device device)
        {
            foreach (var interfaceDescriptor in device.InterfaceDescriptors)
            {
                //@@DisplayInterfaceDescriptor - Interface Descriptor
                builder.AppendLine("\r\n          ===>Interface Descriptor<===");

                //length checked in DisplayConfigDesc()
                builder.AppendLine($"bLength:                           0x{interfaceDescriptor.bLength:X02}");
                builder.AppendLine($"bDescriptorType:                   0x{interfaceDescriptor.bLength:X02}");

                //@@TestCase A5.1
                //@@Priority 1
                //@@Descriptor Field - bInterfaceNumber
                //@@Question - Should we test to verify bInterfaceNumber is valid?
                builder.AppendLine($"bInterfaceNumber:                  0x{interfaceDescriptor.bInterfaceNumber:X02}");

                //@@TestCase A5.2
                //@@Priority 1
                //@@Descriptor Field - bAlternateSetting
                //@@Question - Should we test to verify bAlternateSetting is valid?
                builder.AppendLine($"bAlternateSetting:                 0x{interfaceDescriptor.bAlternateSetting:X02}");

                //@@TestCase A5.3
                //@@Priority 1
                //@@Descriptor Field - bNumEndpoints
                //@@Question - Should we test to verify bNumEndpoints is valid?
                builder.AppendLine($"bNumEndpoints:                     0x{interfaceDescriptor.bNumEndpoints:X02}");

                builder.AppendLine($"bInterfaceClass:                   0x{(int)interfaceDescriptor.bInterfaceClass:X02} -> {interfaceDescriptor.bInterfaceClass}");

                switch (interfaceDescriptor.bInterfaceClass)
                {
                    case UsbSpec.UsbDeviceClass.UsbDeviceClassAudio:
                        builder.AppendLine("  -> Audio Interface Class\r\n");

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");


                        switch (interfaceDescriptor.bInterfaceSubClass)
                        {
                            case UsbDesc.UsbAudioSubclassAudiocontrol:
                                builder.AppendLine("  -> Audio Control Interface SubClass\r\n");
                                break;

                            case UsbDesc.UsbAudioSubclassAudiostreaming:
                                builder.AppendLine("  -> Audio Streaming Interface SubClass\r\n");
                                break;

                            case UsbDesc.UsbAudioSubclassMidistreaming:
                                builder.AppendLine("  -> MIDI Streaming Interface SubClass\r\n");
                                break;

                            default:
                                //@@TestCase A5.4
                                //@@CAUTION
                                //@@Descriptor Field - bInterfaceSubClass
                                //@@Invalid bInterfaceSubClass
                                builder.AppendLine(
                                    "\r\n*!*CAUTION:    This appears to be an invalid bInterfaceSubClass\r\n");
                                break;
                        }


                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassVideo:

                        builder.AppendLine("  -> Video Interface Class\r\n");

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");

                        switch (interfaceDescriptor.bInterfaceSubClass)
                        {
                            case Uvcdesc.VideoSubclassControl:

                                builder.AppendLine("  -> Video Control Interface SubClass");

                                break;

                            case Uvcdesc.VideoSubclassStreaming:

                                builder.AppendLine("  -> Video Streaming Interface SubClass");

                                break;

                            default:
                                //@@TestCase A5.5
                                //@@CAUTION
                                //@@Descriptor Field - bInterfaceSubClass
                                //@@Invalid bInterfaceSubClass
                                builder.AppendLine(
                                    "\r\n*!*CAUTION:    This appears to be an invalid bInterfaceSubClass");
                                break;
                        }

                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassHumanInterface:

                        builder.AppendLine("  -> HID Interface Class");


                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassHub:

                        builder.AppendLine("  -> HUB Interface Class");

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassReserved:
                        //@@TestCase A5.6
                        //@@CAUTION
                        //@@Descriptor Field - bInterfaceClass
                        //@@A reserved USB Device Interface Class has been defined
                        builder.AppendLine(
                            $"\r\n*!*CAUTION:  {UsbSpec.UsbDeviceClass.UsbDeviceClassReserved} is a Reserved USB Device Interface Class");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassCommunications:
                        builder.AppendLine("  -> This is Communications (CDC Control) USB Device Interface Class");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassMonitor:
                        builder.AppendLine(
                            "  -> This is a Monitor USB Device Interface Class*** (This may be obsolete)");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassPhysicalInterface:
                        builder.AppendLine("  -> This is a Physical Interface USB Device Interface Class");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassPower:
                        if (interfaceDescriptor.bInterfaceSubClass == 1 && interfaceDescriptor.bInterfaceProtocol == 1)
                        {
                            builder.AppendLine("  -> This is an Image USB Device Interface Class");
                        }
                        else
                        {
                            builder.AppendLine(
                                "  -> This is a Power USB Device Interface Class (This may be obsolete)");
                        }

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassPrinter:
                        builder.AppendLine("  -> This is a Printer USB Device Interface Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassStorage:
                        builder.AppendLine("  -> This is a Mass Storage USB Device Interface Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassCdcData:
                        builder.AppendLine("  -> This is a CDC Data USB Device Interface Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassSmartCard:
                        builder.AppendLine("  -> This is a Chip/Smart Card USB Device Interface Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassContentSecurity:
                        builder.AppendLine("  -> This is a Content Security USB Device Interface Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassDiagnosticDevice:
                        if (interfaceDescriptor.bInterfaceSubClass == 1 && interfaceDescriptor.bInterfaceProtocol == 1)
                        {
                            builder.AppendLine(
                                "  -> This is a Reprogrammable USB2 Compliance Diagnostic Device USB Device\r\n");
                        }
                        else
                        {
                            //@@TestCase A5.7
                            //@@CAUTION
                            //@@Descriptor Field - bInterfaceClass
                            //@@Invalid Interface Class
                            builder.AppendLine("\r\n*!*CAUTION:    This appears to be an invalid Interface Class\r\n");
                        }

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassWirelessController:
                        if (interfaceDescriptor.bInterfaceSubClass == 1 && interfaceDescriptor.bInterfaceProtocol == 1)
                        {
                            builder.AppendLine(
                                "  -> This is a Wireless RF Controller USB Device Interface Class with Bluetooth Programming Interface\r\n");
                        }
                        else
                        {
                            //@@TestCase A5.8
                            //@@CAUTION
                            //@@Descriptor Field - bInterfaceClass
                            //@@Invalid Interface Class
                            builder.AppendLine("\r\n*!*CAUTION:    This appears to be an invalid Interface Class\r\n");
                        }

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;

                    case UsbSpec.UsbDeviceClass.UsbDeviceClassApplicationSpecific:
                        builder.AppendLine("  -> This is an Application Specific USB Device Interface Class\r\n");

                        switch (interfaceDescriptor.bInterfaceSubClass)
                        {
                            case 1:
                                builder.AppendLine(
                                    "  -> This is a Device Firmware Application Specific USB Device Interface Class\r\n");
                                break;
                            case 2:
                                builder.AppendLine(
                                    "  -> This is an IrDA Bridge Application Specific USB Device Interface Class\r\n");
                                break;
                            case 3:
                                builder.AppendLine(
                                    "  -> This is a Test & Measurement Class (USBTMC) Application Specific USB Device Interface Class\r\n");
                                break;
                            default:
                                //@@TestCase A5.9
                                //@@CAUTION
                                //@@Descriptor Field - bInterfaceClass
                                //@@Invalid Interface Class
                                builder.AppendLine(
                                    "\r\n*!*CAUTION:    This appears to be an invalid Interface Class\r\n");
                                break;
                        }

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;
                    case UsbSpec.UsbDeviceClass.UsbDeviceClassBillboard:
                        builder.AppendLine("  -> Billboard Class\r\n");
                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        switch (interfaceDescriptor.bInterfaceSubClass)
                        {
                            case 0:
                                builder.AppendLine("  -> Billboard Subclass");
                                break;
                            default:
                                builder.AppendLine(
                                    "\r\n*!*CAUTION:    This appears to be an invalid bInterfaceSubClass");
                                break;
                        }

                        break;

                    default:

                        builder.AppendLine("  -> Interface Class Unknown to USBView");

                        builder.AppendLine(
                            $"bInterfaceSubClass:                0x{interfaceDescriptor.bInterfaceSubClass:X02}");
                        break;
                }

                builder.AppendLine(
                    $"bInterfaceProtocol:                0x{interfaceDescriptor.bInterfaceProtocol:X02}");

                //This is basically the check for PC_PROTOCOL_UNDEFINED
                if ((interfaceDescriptor.bInterfaceClass == UsbSpec.UsbDeviceClass.UsbDeviceClassVideo) ||
                    (interfaceDescriptor.bInterfaceClass == UsbSpec.UsbDeviceClass.UsbDeviceClassAudioVideo))
                {
                    if (interfaceDescriptor.bInterfaceProtocol != PcProtocolUndefined)
                    {
                        //@@TestCase A5.10
                        //@@WARNING
                        //@@Descriptor Field - iInterface
                        //@@bInterfaceProtocol must be set to PC_PROTOCOL_UNDEFINED
                        builder.AppendLine(
                            $"*!*WARNING:  must be set to PC_PROTOCOL_UNDEFINED {PcProtocolUndefined} for this class\r\n");
                    }
                }

                builder.AppendLine($"iInterface:                        0x{interfaceDescriptor.iInterface:X02}");

#warning TODO
                //if (interfaceDescriptor.iInterface> 0)
                //{
                //    DisplayStringDescriptor(interfaceDescriptor.iInterface,
                //        StringDescs,
                //        LatestDevicePowerState);
                //}

#warning TODO
                //if (interfaceDescriptor.bLength == sizeof(USB_INTERFACE_DESCRIPTOR2))
                //{
                //    PUSB_INTERFACE_DESCRIPTOR2 interfaceDesc2;

                //    interfaceDesc2 = (PUSB_INTERFACE_DESCRIPTOR2) InterfaceDesc;

                //    builder.AppendLine("wNumClasses:                     0x%04X\r\n",
                //        interfaceDesc2->wNumClasses);
                //}
            }
        }

        private static void AppendConfigurationDescriptors(StringBuilder builder, Device device)
        {
            uint count = 0;


            var isSuperSpeed = device.NodeConnectionInfoV2.Flags == UsbIoControl.UsbNodeConnectionInformationExV2Flags
                                   .DeviceIsOperatingAtSuperSpeedOrHigher ||
                               device.NodeConnectionInfoV2.Flags == UsbIoControl.UsbNodeConnectionInformationExV2Flags
                                   .DeviceIsOperatingAtSuperSpeedPlusOrHigher;

            foreach (var configurationDescriptor in device.ConfigurationDescriptors)
            {
                builder.AppendLine("\r\n          ===>Configuration Descriptor<===");
                //@@DisplayConfigurationDescriptor - Configuration Descriptor

                //length checked in DisplayConfigDesc()

                builder.AppendLine($"bLength:                           0x{configurationDescriptor.bLength:X02}");
                builder.AppendLine(
                    $"bDescriptorType:                   0x{(int) configurationDescriptor.bDescriptorType:X02} => {configurationDescriptor.bDescriptorType}");

                //@@TestCase A4.1
                //@@Priority 1
                //@@Descriptor Field - wTotalLength
                //@@Verify Configuration length is valid
                builder.AppendLine($"wTotalLength:                    0x{configurationDescriptor.wTotalLength:X04}");

                //count = GetConfigurationSize(device);
                //if (count != configurationDescriptor.wTotalLength)
                //{
                //    builder.AppendLine($"*!*ERROR: Invalid total configuration size 0x{configurationDescriptor.wTotalLength:X02}, should be 0x{count:X02}");
                //}
                //else
                //{
                //    builder.AppendLine("  -> Validated\r\n");
                //}

                //@@TestCase A4.2
                //@@Priority 1
                //@@Descriptor Field - bNumInterfaces
                //@@Verify the number of interfaces is valid
                builder.AppendLine(
                    $"bNumInterfaces:                    0x{configurationDescriptor.bNumInterfaces:X02}");
                builder.AppendLine(
                    $"bConfigurationValue:               0x{configurationDescriptor.bConfigurationValue:X02}");

                if (configurationDescriptor.bConfigurationValue != 1)
                {
                    //@@TestCase A4.3
                    //@@CAUTION
                    //@@Descriptor Field - bConfigurationValue
                    //@@Most host controllers do not handle more than one configuration
                    builder.AppendLine(
                        "*!*CAUTION:    Most host controllers will only work with one configuration per speed");
                }

                builder.AppendLine(
                    $"bConfigurationValue:               0x{configurationDescriptor.bConfigurationValue:X02}");

#warning TODO
                //if (configurationDescriptor.iConfiguration)
                //{
                //    DisplayStringDescriptor(configurationDescriptor.iConfiguration,
                //        StringDescs,
                //        info->DeviceInfoNode != NULL ? info->DeviceInfoNode->LatestDevicePowerState : PowerDeviceUnspecified);
                //}
                builder.AppendLine(
                    $"bmAttributes:                      0x{(int) configurationDescriptor.bmAttributes:X02} -> {configurationDescriptor.bmAttributes}");

                if (device.NodeConnectionInfo.DeviceDescriptor.bcdUSB == 0x0100)
                {
                    if (configurationDescriptor.bmAttributes.IsSet(UsbSpec.UsbConfiguration.UsbConfigSelfPowered))
                    {
                        builder.AppendLine("  -> Self Powered");
                    }

                    if (configurationDescriptor.bmAttributes.IsSet(UsbSpec.UsbConfiguration.UsbConfigBusPowered))
                    {
                        builder.AppendLine("  -> Bus Powered");
                    }
                }
                else
                {
                    builder.AppendLine(
                        configurationDescriptor.bmAttributes.IsSet(UsbSpec.UsbConfiguration.UsbConfigSelfPowered)
                            ? "  -> Self Powered"
                            : "  -> Bus Powered");

                    if ((configurationDescriptor.bmAttributes & UsbSpec.UsbConfiguration.UsbConfigBusPowered) == 0)
                    {
                        builder.AppendLine("\r\n*!*ERROR:    Bit 7 is reserved and must be set");
                    }
                }

                if (configurationDescriptor.bmAttributes.IsSet(UsbSpec.UsbConfiguration.UsbConfigRemoteWakeup))
                {
                    builder.AppendLine("  -> Remote Wakeup");
                }

                if ((configurationDescriptor.bmAttributes & UsbSpec.UsbConfiguration.UsbConfigReserved) != 0)
                {
                    //@@TestCase A4.4
                    //@@WARNING
                    //@@Descriptor Field - bmAttributes
                    //@@A bit has been set in reserved space
                    builder.AppendLine("*!*ERROR:    Bits 4...0 are reserved");
                }

                builder.Append($"MaxPower:                          0x{configurationDescriptor.MaxPower:X02}");

                var power = isSuperSpeed ? configurationDescriptor.MaxPower * 8 : configurationDescriptor.MaxPower * 2;
                builder.AppendLine($" = %{power:d3} mA\r\n");
            }
        }


        private static void AppendDeviceDescriptor(StringBuilder builder, Device device)
        {
            UsbSpec.UsbDeviceDescriptor deviceDescriptor = device.DeviceDescriptor;
            UsbIoControl.UsbNodeConnectionInformationEx connectInfo = device.NodeConnectionInfo;

            bool tog = true;
            uint iaDcount = 0;

            builder.AppendLine("\r\n          ===>Device Descriptor<===");
            builder.AppendLine($"bLength:                           0x{deviceDescriptor.bLength:X02}");
            builder.AppendLine(
                $"bDescriptorType:                   0x{(int) deviceDescriptor.bDescriptorType:X02} -> {deviceDescriptor.bDescriptorType}");
            builder.AppendLine($"bcdUSB:                          0x{deviceDescriptor.bcdUSB:X04}");
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

            builder.Append($"bDeviceSubClass:                   0x{connectInfo.DeviceDescriptor.bDeviceSubClass:X02}");

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
            }

            builder.AppendLine(
                $"bMaxPacketSize0:                   0x{connectInfo.DeviceDescriptor.bMaxPacketSize0:X02} = ({connectInfo.DeviceDescriptor.bMaxPacketSize0}) Bytes");

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

        private static void AppendNodeConnectionInfoExV2(StringBuilder builder,
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

        private static void AppendUsbPortConnectorProperties(StringBuilder builder,
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
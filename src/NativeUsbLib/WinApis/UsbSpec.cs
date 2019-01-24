using System;
using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public static class UsbSpec
    {
        public enum EndpointIsochronousUsage
        {
            IsochronousUsageDataEndoint = 0x00,
            IsochronousUsageFeedbackEndpoint = 0x10,
            IsochronousUsageImplicitFeedbackDataEndpoint = 0x20,
            IsochronousUsageReserved = 0x30
        }

        public enum EndpointSynchronization
        {
            IsochronousSynchronizationNoSynchronization = 0x00,
            IsochronousSynchronizationAsynchronous = 0x04,
            IsochronousSynchronizationAdaptive = 0x08,
            IsochronousSynchronizationSynchronous = 0x0C
        }

        [Flags]
        public enum UsbConfiguration : byte
        {
            UsbConfigPoweredMask = 0xC0,
            UsbConfigBusPowered = 0x80,
            UsbConfigSelfPowered = 0x40,
            UsbConfigRemoteWakeup = 0x20,
            UsbConfigReserved = 0x1F
        }

        public enum UsbDescriptorType : byte
        {
            DeviceDescriptorType = 0x1,
            ConfigurationDescriptorType = 0x2,
            StringDescriptorType = 0x3,
            InterfaceDescriptorType = 0x4,
            EndpointDescriptorType = 0x5,
            Hub20Descriptor = 0x29,
            Hub30Descriptor = 0x2A
        }

        //
        // With the exception of the HUB device class, USB class codes are not
        // defined in the core USB 1.1, 2.0, 3.0 specifications.
        //
        public enum UsbDeviceClass : byte
        {
            UsbDeviceClassReserved = 0x00,
            UsbDeviceClassAudio = 0x01,
            UsbDeviceClassCommunications = 0x02,
            UsbDeviceClassHumanInterface = 0x03,
            UsbDeviceClassMonitor = 0x04,
            UsbDeviceClassPhysicalInterface = 0x05,
            UsbDeviceClassPower = 0x06,
            UsbDeviceClassImage = 0x06,
            UsbDeviceClassPrinter = 0x07,
            UsbDeviceClassStorage = 0x08,
            UsbDeviceClassHub = 0x09,
            UsbDeviceClassCdcData = 0x0A,
            UsbDeviceClassSmartCard = 0x0B,
            UsbDeviceClassContentSecurity = 0x0D,
            UsbDeviceClassVideo = 0x0E,
            UsbDeviceClassPersonalHealthcare = 0x0F,
            UsbDeviceClassAudioVideo = 0x10,
            UsbDeviceClassBillboard = 0x11,
            UsbDeviceClassDiagnosticDevice = 0xDC,
            UsbDeviceClassWirelessController = 0xE0,
            UsbDeviceClassMiscellaneous = 0xEF,
            UsbDeviceClassApplicationSpecific = 0xFE,
            UsbDeviceClassVendorSpecific = 0xFF
        }

        public enum UsbDeviceSpeed : byte
        {
            UsbLowSpeed,
            UsbFullSpeed,
            UsbHighSpeed,
            UsbSuperSpeed
        }

        public enum UsbEndpointType : byte
        {
            Control = 0x0,
            Isochronous = 0x1,
            Bulk = 0x2,
            Interrupt = 0x3
        }

        public const int MaximumUsbStringLength = 255;

        //
        // USB_ENDPOINT_DESCRIPTOR bEndpointAddress bit 7
        //
        public const int UsbEndpointDirectionMask = 0x80;
        public const int UsbEndpointAddressMask = 0x0F;

        //
        // USB_ENDPOINT_DESCRIPTOR bmAttributes bits 0-1
        //
        public const int UsbEndpointTypeMask = 0x03;
        public const int UsbEndpointTypeControl = 0x00;
        public const int UsbEndpointTypeIsochronous = 0x01;
        public const int UsbEndpointTypeBulk = 0x02;
        public const int UsbEndpointTypeInterrupt = 0x03;

        //
        // USB_ENDPOINT_DESCRIPTOR bmAttributes bits 7-2
        //
        public const int UsbEndpointTypeBulkReservedMask = 0xFC;
        public const int UsbEndpointTypeControlReservedMask = 0xFC;
        public const int Usb20EndpointTypeInterruptReservedMask = 0xFC;
        public const int Usb30EndpointTypeInterruptReservedMask = 0xCC;
        public const int UsbEndpointTypeIsochronousReservedMask = 0xC0;

        public const int Usb30EndpointTypeInterruptUsageMask = 0x30;
        public const int Usb30EndpointTypeInterruptUsagePeriodic = 0x00;
        public const int Usb30EndpointTypeInterruptUsageNotification = 0x10;
        public const int Usb30EndpointTypeInterruptUsageReserved10 = 0x20;
        public const int Usb30EndpointTypeInterruptUsageReserved11 = 0x30;

        public const int UsbEndpointTypeIsochronousSynchronizationMask = 0x0C;
        public const int UsbEndpointTypeIsochronousSynchronizationNoSynchronization = 0x00;
        public const int UsbEndpointTypeIsochronousSynchronizationAsynchronous = 0x04;
        public const int UsbEndpointTypeIsochronousSynchronizationAdaptive = 0x08;
        public const int UsbEndpointTypeIsochronousSynchronizationSynchronous = 0x0C;

        public const int UsbEndpointTypeIsochronousUsageMask = 0x30;
        public const int UsbEndpointTypeIsochronousUsageDataEndoint = 0x00;
        public const int UsbEndpointTypeIsochronousUsageFeedbackEndpoint = 0x10;
        public const int UsbEndpointTypeIsochronousUsageImplicitFeedbackDataEndpoint = 0x20;
        public const int UsbEndpointTypeIsochronousUsageReserved = 0x30;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbStringDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaximumUsbStringLength)]
            public string String;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbEndpointDescriptor
        {
            public byte bLength;
            public UsbDescriptorType bDescriptorType;
            public byte bEndpointAddress;
            public byte bmAttributes;
            public short wMaxPacketSize;
            public byte bInterval;

            //public bool IsEndpointOut()
            //{
            //    return (bEndpointAddress & UsbEndpointDirectionMask) > 0;
            //}

            //public bool IsEndpointIn()
            //{
            //    return (bEndpointAddress & UsbEndpointDirectionMask) == 0;
            //}

            //public byte GetEndpointId()
            //{
            //    return (byte) (bEndpointAddress & UsbEndpointAddressMask);
            //}

            //public UsbEndpointType GetEndpointType()
            //{
            //    return (UsbEndpointType) (bmAttributes & UsbEndpointTypeMask);
            //}

            //public EndpointSynchronization GetSynchronization()
            //{
            //    return (EndpointSynchronization) (bmAttributes &
            //                                      UsbEndpointTypeIsochronousSynchronizationMask);
            //}

            //public EndpointIsochronousUsage GetIsochronousUsage()
            //{
            //    return (EndpointIsochronousUsage)(bmAttributes &
            //                                     UsbEndpointTypeIsochronousUsageMask);
            //}
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UsbDeviceDescriptor
        {
            public byte bLength;
            public UsbDescriptorType bDescriptorType;
            public ushort bcdUSB;
            public UsbDesc.DeviceClassType bDeviceClass;
            public byte bDeviceSubClass;
            public byte bDeviceProtocol;
            public byte bMaxPacketSize0;
            public ushort idVendor;
            public ushort idProduct;
            public ushort bcdDevice;
            public byte iManufacturer;
            public byte iProduct;
            public byte iSerialNumber;
            public byte bNumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbHubDescriptor
        {
            public byte DescriptorLength;
            public UsbDescriptorType DescriptorType;
            public byte NumberOfPorts;
            public short HubCharacteristics;
            public byte PowerOnToPowerGood;
            public byte HubControlCurrent;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] RemoveAndPowerMask;
        }

        //
        // USB 1.1: 9.6.2 Configuration, Table 9-8. Standard Configuration Descriptor
        // USB 2.0: 9.6.3 Configuration, Table 9-10. Standard Configuration Descriptor
        // USB 3.0: 9.6.3 Configuration, Table 9-15. Standard Configuration Descriptor
        //
        [StructLayout(LayoutKind.Sequential)]
        public struct UsbConfigurationDescriptor
        {
            public byte bLength;
            public UsbDescriptorType bDescriptorType;
            public ushort wTotalLength;
            public byte bNumInterfaces;
            public byte bConfigurationValue;
            public byte iConfiguration;
            public UsbConfiguration bmAttributes;
            public byte MaxPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbInterfaceDescriptor
        {
            public byte bLength;
            public UsbDescriptorType bDescriptorType;
            public byte bInterfaceNumber;
            public byte bAlternateSetting;
            public byte bNumEndpoints;
            public UsbDeviceClass bInterfaceClass;
            public byte bInterfaceSubClass;
            public byte bInterfaceProtocol;
            public byte iInterface;
        }
    }
}
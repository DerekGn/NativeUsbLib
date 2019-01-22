using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public static class UsbSpec
    {
        public const int MaximumUsbStringLength = 255;

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

        public enum UsbDeviceClass : byte
        {
            Reserved = 0x00,
            Audio = 0x01,
            Communications = 0x02,
            HumanInterface = 0x03,
            Monitor = 0x04,
            PhysicalInterface = 0x05,
            Power = 0x06,
            Image = 0x06,
            Printer = 0x07,
            Storage = 0x08,
            Hub = 0x09,
            CdcData = 0x0A,
            SmartCard = 0x0B,
            ContentSecurity = 0x0D,
            Video = 0x0E,
            PersonalHealthcare = 0x0F,
            AudioVideo = 0x10,
            Billboard = 0x11,
            DiagnosticDevice = 0xDC,
            WirelessController = 0xE0,
            Miscellaneous = 0xEF,
            ApplicationSpecific = 0xFE,
            VendorSpecific = 0xFF
        }

        public enum UsbDeviceSpeed : byte
        {
            UsbLowSpeed,
            UsbFullSpeed,
            UsbHighSpeed,
            UsbSuperSpeed
        }

        public enum UsbTransfer : byte
        {
            Control = 0x0,
            Isochronous = 0x1,
            Bulk = 0x2,
            Interrupt = 0x3
        }

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
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public byte EndpointAddress;
            public UsbTransfer Attributes;
            public short MaxPacketSize;
            public byte Interval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UsbDeviceDescriptor
        {
            public short bcdDevice;
            public short bcdUSB;
            public UsbDeviceClass bDeviceClass;
            public UsbDescriptorType DescriptorType;
            public byte DeviceProtocol;
            public byte DeviceSubClass;
            public ushort IdProduct;
            public ushort IdVendor;
            public byte IManufacturer;
            public byte IProduct;
            public byte ISerialNumber;
            public byte Length;
            public byte MaxPacketSize0;
            public byte NumConfigurations;
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
    }
}
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public static class UsbDesc
    {
        public enum DeviceClassType : byte
        {
            UsbInterfaceClassDevice = 0x00,
            UsbCommunicationDevice = 0x02,
            UsbHubDevice = 0x09,
            UsbDeviceClassBillboard = 0x11,
            UsbDiagnosticDevice = 0xDC,
            UsbWirelessControllerDevice = 0xE0,
            UsbMiscellaneousDevice = 0xEF,
            UsbVendorSpecificDevice = 0xFF
        }

        //
        //Device Descriptor bDeviceSubClass values
        //
        public const int UsbCommonSubClass = 0x02;

        //
        //IAD protocol values
        //
        public const int UsbIadProtocol = 0x01;

        //
        // USB Device Class Definition for Audio Devices
        // Appendix A.  Audio Device Class Codes
        //

        // A.2  Audio Interface Subclass Codes
        //
        public const byte UsbAudioSubclassUndefined = 0x00;
        public const byte UsbAudioSubclassAudiocontrol = 0x01;
        public const byte UsbAudioSubclassAudiostreaming = 0x02;
        public const byte UsbAudioSubclassMidistreaming = 0x03;

        // HID Class HID Descriptor
        //
        [StructLayout(LayoutKind.Sequential)]
        public struct HidDescriptor
        {
            public byte bLength;
            public UsbSpec.UsbDescriptorType bDescriptorType;
            public short BcdHid;
            public byte bCountryCode;
            public byte bNumDescriptors;
            //public List<OptionalDescriptor> OptionalDescriptors;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OptionalDescriptor
        {
            public byte bDescriptorType;
            public short wDescriptorLength;
        }
    }
}
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
    }
}
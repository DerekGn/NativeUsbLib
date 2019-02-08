namespace NativeUsbLib.WinApis
{
    public static class Uvcdesc
    {
        public const byte UsbDeviceClassVideo = 0x0E;

        // Video sub-classes
        public const byte SubclassUndefined = 0x00;
        public const byte VideoSubclassControl = 0x01;
        public const byte VideoSubclassStreaming = 0x02;
    }
}
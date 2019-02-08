namespace NativeUsbLib.WinApis
{
    internal class DevIoControl
    {
        public const int MethodBuffered = 0;
        public const int MethodInDirect = 1;
        public const int MethodOutDirect = 2;
        public const int MethodNeither = 3;

        public const int FileDeviceUnknown = 0x00000022;

        public const int FileAnyAccess = 0;
        public const int FileSpecialAccess = FileAnyAccess;
        public const int FileReadAccess = 0x0001;    // file & pipe
        public const int FileWriteAccess = 0x0002;
    }
}
namespace NativeUsbLib.WinApis
{
    public static class WinNtApi
    {
        public const ulong Delete = 0x00010000L;
        public const ulong ReadControl = 0x00020000L;
        public const ulong WriteDac = 0x00040000L;
        public const ulong WriteOwner = 0x00080000L;
        public const ulong Synchronize = 0x00100000L;

        public const ulong StandardRightsRequired = 0x000F0000L;

        public const ulong StandardRightsRead = ReadControl;
        public const ulong StandardRightSWrite = (ReadControl);
        public const ulong StandardRightsExecute = (ReadControl);

        public const ulong StandardRightsAll = (0x001F0000L);

        public const ulong SpecificRightsAll = (0x0000FFFFL);

        public const int KeyQueryValue = 0x0001;
        public const int KeySetValue = 0x0002;
        public const int KeyCREATE_SUB_KEY = 0x0004;
        public const int KeyEnumerateSubKeys = 0x0008;
        public const int KeyNotify = 0x0010;
        public const int KeyCreateLink = 0x0020;
        public const int KeyWOW6432Key = 0x0200;
        public const int KeyWOW6464Key = 0x0100;
        public const int KeyWOW64Res = 0x0300;

        public const ulong KeyRead = ((StandardRightsRead | 
            KeyQueryValue |
            KeyEnumerateSubKeys |
            KeyNotify) &
            (~Synchronize));
    }
}

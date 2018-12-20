namespace NativeUsbLib.WinApis
{
    internal static class UsbIoDefinitions
    {
        public const int HcdGetStats1 = 255;
        public const int HcdDiagnosticModeOn = 256;
        public const int HcdDiagnosticModeOff = 257;
        public const int HcdGetRootHubName = 258;
        public const int HcdGetDriverkeyName = 265;
        public const int HcdGetStats2 = 266;
        public const int HcdDisablePort = 268;
        public const int HcdEnablePort = 269;
        public const int HcdUserRequest = 270;
        public const int HcdTraceReadRequest = 275;


        public const int UsbGetNodeInformation = 258;
        public const int UsbGetNodeConnectionInformation = 259;
        public const int UsbGetDescriptorFromNodeConnection = 260;
        public const int UsbGetNodeConnectionName = 261;
        public const int UsbDiagIgnoreHubsOn = 262;
        public const int UsbDiagIgnoreHubsOff = 263;
        public const int UsbGetNodeConnectionDriverkeyName = 264;
        public const int UsbGetHubCapabilities = 271;
        public const int UsbGetNodeConnectionAttributes = 272;
        public const int UsbHubCyclePort = 273;
        public const int UsbGetNodeConnectionInformationEx = 274;
        public const int UsbResetHub = 275;
        public const int UsbGetHubCapabilitiesEx = 276;
        public const int UsbGetHubInformationEx = 277;
        public const int UsbGetPortConnectorProperties = 278;
        public const int UsbGetNodeConnectionInformationExV2 = 279;

        public const int FileDeviceUsb = DevIoControl.FileDeviceUnknown;
    }
}
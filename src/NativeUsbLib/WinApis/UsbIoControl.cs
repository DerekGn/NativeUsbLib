namespace NativeUsbLib.WinApis
{
    internal static class UsbIoControl
    {
        public const int IoctlUsbGetRootHubName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                  (DevIoControl.FileAnyAccess << 14) |
                                                  (UsbIoDefinitions.UsbGetNodeInformation << 2) |
                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeInformation = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                      (DevIoControl.FileAnyAccess << 14) |
                                                      (UsbIoDefinitions.UsbGetNodeInformation << 2) |
                                                      DevIoControl.MethodBuffered;

        public const int IoctlUsbGetHubInformationEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                       (DevIoControl.FileAnyAccess << 14) |
                                                       (UsbIoDefinitions.UsbGetHubInformationEx << 2) |
                                                       DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionInformationEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                  (DevIoControl.FileAnyAccess << 14) |
                                                                  (UsbIoDefinitions.UsbGetNodeConnectionInformationEx <<
                                                                   2) |
                                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetDescriptorFromNodeConnection = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                   (DevIoControl.FileAnyAccess << 14) |
                                                                   (UsbIoDefinitions
                                                                        .UsbGetDescriptorFromNodeConnection << 2) |
                                                                   DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                         (DevIoControl.FileAnyAccess << 14) |
                                                         (UsbIoDefinitions.UsbGetNodeConnectionName << 2) |
                                                         DevIoControl.MethodBuffered;

        public const int IoctlUsbGetNodeConnectionDriverkeyName = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                                  (DevIoControl.FileAnyAccess << 14) |
                                                                  (UsbIoDefinitions.UsbGetNodeConnectionDriverkeyName <<
                                                                   2) |
                                                                  DevIoControl.MethodBuffered;

        public const int IoctlUsbGetHubCapabilitiesEx = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                        (DevIoControl.FileAnyAccess << 14) |
                                                        (UsbIoDefinitions.UsbGetHubCapabilitiesEx << 2) |
                                                        DevIoControl.MethodBuffered;
    }
}
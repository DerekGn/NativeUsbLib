using System.Diagnostics;

namespace NativeUsbLib.Diagnostics
{
    public static class CoreTraceSource
    {
        public static TraceSource Source => new TraceSource(nameof(NativeUsbLib));

        public const int DeviceSourceId = 1;
        public const int DeviceFactorySourceId = 2;
        public const int UsbControllerSourceId = 3;
        public const int UsbHubSourceId = 4;
    }
}

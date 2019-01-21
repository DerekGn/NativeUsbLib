using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public class UsbUser
    {
        public const int IoctlUsbUserRequest = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                                        (DevIoControl.FileAnyAccess << 14) |
                                                        (UsbIoDefinitions.HcdUserRequest << 2) |
                                                        DevIoControl.MethodBuffered;

        public enum WdmusbPowerState
        {
            WdmUsbPowerNotMapped = 0,

            WdmUsbPowerSystemUnspecified = 100,
            WdmUsbPowerSystemWorking,
            WdmUsbPowerSystemSleeping1,
            WdmUsbPowerSystemSleeping2,
            WdmUsbPowerSystemSleeping3,
            WdmUsbPowerSystemHibernate,
            WdmUsbPowerSystemShutdown,

            WdmUsbPowerDeviceUnspecified = 200,
            WdmUsbPowerDeviceD0,
            WdmUsbPowerDeviceD1,
            WdmUsbPowerDeviceD2,
            WdmUsbPowerDeviceD3
        }

        public const int UsbuserGetControllerInfo0 = 0x00000001;
        public const int UsbuserGetControllerDriverKey = 0x00000002;
        public const int UsbuserPassThru = 0x00000003;
        public const int UsbuserGetPowerStateMap = 0x00000004;
        public const int UsbuserGetBandwidthInformation = 0x00000005;
        public const int UsbuserGetBusStatistics0 = 0x00000006;
        public const int UsbuserGetRoothubSymbolicName = 0x00000007;
        public const int UsbuserGetUsbDriverVersion = 0x00000008;
        public const int UsbuserGetUsb2HwVersion = 0x00000009;
        public const int UsbuserUsbRefreshHctReg = 0x0000000a;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbPowerInfo
        {
            /* input */
            public WdmusbPowerState SystemState;

            /* output */
            public WdmusbPowerState HcDevicePowerState;
            public WdmusbPowerState HcDeviceWake;
            public WdmusbPowerState HcSystemWake;

            public WdmusbPowerState RhDevicePowerState;
            public WdmusbPowerState RhDeviceWake;
            public WdmusbPowerState RhSystemWake;

            public WdmusbPowerState LastSystemSleepState;

            public byte CanWakeup;
            public byte IsPowered;
        }

        internal enum UsbUserErrorCode
        {
            UsbUserSuccess = 0,
            UsbUserNotSupported,
            UsbUserInvalidRequestCode,
            UsbUserFeatureDisabled,
            UsbUserInvalidHeaderParameter,
            UsbUserInvalidParameter,
            UsbUserMiniportError,
            UsbUserBufferTooSmall,
            UsbUserErrorNotMapped,
            UsbUserDeviceNotStarted,
            UsbUserNoDeviceConnected
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UsbuserRequestHeader
        {
            /*
                API Requested
            */
            public uint UsbUserRequest;

            /*
                status code returned by port driver
            */
            public UsbUserErrorCode UsbUserStatusCode;

            /*
                size of client input/output buffer
                we always use the same buffer for input
                and output
            */
            public uint RequestBufferLength;

            /*
                size of buffer required to get all of the data
            */
            public uint ActualBufferLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct UsbuserPowerInfoRequest
        {
            public UsbuserRequestHeader Header;
            public UsbPowerInfo PowerInformation;
        }
    }
}
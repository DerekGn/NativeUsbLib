using System.Runtime.InteropServices;

namespace NativeUsbLib.WinApis
{
    public class UsbUser
    {
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

        public const int IoctlUsbUserRequest = (UsbIoDefinitions.FileDeviceUsb << 16) |
                                               (DevIoControl.FileAnyAccess << 14) |
                                               (UsbIoDefinitions.HcdUserRequest << 2) |
                                               DevIoControl.MethodBuffered;

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

        public enum UsbControllerFlavor
        {
            UsbHcGeneric = 0,

            OhciGeneric = 100,
            OhciHydra,
            OhciNec,

            UhciGeneric = 200,
            UhciPiix4 = 201,
            UhciPiix3 = 202,
            UhciIch2 = 203,
            UhciReserved204 = 204,
            UhciIch1 = 205,
            UhciIch3M = 206,
            UhciIch4 = 207,
            UhciIch5 = 208,
            UhciIch6 = 209,

            UhciIntel = 249,

            UhciVia = 250,
            UhciViaX01 = 251,
            UhciViaX02 = 252,
            UhciViaX03 = 253,
            UhciViaX04 = 254,

            UhciViaX0EFifo = 264,

            EhciGeneric = 1000,
            EhciNec = 2000,
            EhciLucent = 3000,
            EhciNvidiaTegra2 = 4000,
            EhciNvidiaTegra3 = 4001,
            EhciIntelMedfield = 5001
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct UsbuserControllerInfo0
        {
            public UsbuserRequestHeader Header;
            public UsbControllerInfo0 Info0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbControllerInfo0
        {
            public int PciVendorId;
            public int PciDeviceId;
            public int PciRevision;

            public int NumberOfRootPorts;

            public UsbControllerFlavor ControllerFlavor;

            public int HcFeatureFlags;
        }
    }
}
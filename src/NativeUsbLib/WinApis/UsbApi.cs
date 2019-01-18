using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NativeUsbLib.WinApis.Marshalling;

namespace NativeUsbLib.WinApis
{
    public class UsbApi
    {
        #region "WinAPI"

        // *******************************************************************************************
        // *************************************** constants *****************************************
        // *******************************************************************************************

        #region constants

        public const uint GenericRead = 0x80000000;
        public const uint GenericWrite = 0x40000000;
        public const uint GenericExecute = 0x20000000;
        public const uint GenericAll = 0x10000000;
        public const uint FileFlagNoBuffering = 0x20000000;
        public const uint FileFlagOverlapped = 0x40000000;

        public const int FileShareRead = 0x1;
        public const int FileShareWrite = 0x2;
        public const int OpenExisting = 0x3;
        public const int InvalidHandleValue = -1;

        public const int UsbuserGetControllerInfo0 = 0x00000001;
        public const int UsbuserGetControllerDriverKey = 0x00000002;

        public const int IoctlGetHcdDriverkeyName = 0x220424;
        public const int IoctlStorageGetDeviceNumber = 0x2D1080;

        public const int UsbDeviceDescriptorType = 0x1;
        public const int UsbConfigurationDescriptorType = 0x2;
        public const int UsbStringDescriptorType = 0x3;
        public const int UsbInterfaceDescriptorType = 0x4;
        public const int UsbEndpointDescriptorType = 0x5;

        public const string GuidDevinterfaceHubcontroller = "3abf6f2d-71c4-462a-8a92-1e6861e6af27";
        public const int MaxBufferSize = 2048;
        public const int MaximumUsbStringLength = 255;
        public const string RegstrKeyUsb = "USB";
        public const int RegSz = 1;
        public const int DifPropertychange = 0x00000012;
        public const int DicsFlagGlobal = 0x00000001;

        public const int DigcfDefault = 0x00000001; // only valid with DIGCF_DEVICEINTERFACE
        public const int DigcfPresent = 0x00000002;
        public const int DigcfAllclasses = 0x00000004;
        public const int DigcfProfile = 0x00000008;
        public const int DigcfDeviceinterface = 0x00000010;

        public const int SpdrpDriver = 0x9;
        public const int SpdrpDevicedesc = 0x0;

        public const int DicsEnable = 0x00000001;
        public const int DicsDisable = 0x00000002;

        #endregion

        // *******************************************************************************************
        // ************************************* enumerations ****************************************
        // *******************************************************************************************

        #region enumerations

        public enum UsbDeviceClass : byte
        {
            UnspecifiedDevice = 0x00,
            AudioInterface = 0x01,
            CommunicationsAndCdcControlBoth = 0x02,
            HidInterface = 0x03,
            PhysicalInterfaceDevice = 0x5,
            ImageInterface = 0x06,
            PrinterInterface = 0x07,
            MassStorageInterface = 0x08,
            HubDevice = 0x09,
            CdcDataInterface = 0x0A,
            SmartCardInterface = 0x0B,
            ContentSecurityInterface = 0x0D,
            VidioInterface = 0x0E,
            PersonalHeathcareInterface = 0x0F,
            DiagnosticDeviceBoth = 0xDC,
            WirelessControllerInterface = 0xE0,
            MiscellaneousBoth = 0xEF,
            ApplicationSpecificInterface = 0xFE,
            VendorSpecificBoth = 0xFF
        }

        public enum HubCharacteristics : byte
        {
            GangedPowerSwitching = 0x00,
            IndividualPotPowerSwitching = 0x01,
            // to do
        }

        public enum UsbHubNode
        {
            UsbHub,
            UsbMiParent
        }

        public enum UsbHubType
        {
            UsbRootHub = 1,
            Usb20Hub = 2,
            Usb30Hub = 3
        }

        public enum UsbDescriptorType : byte
        {
            DeviceDescriptorType = 0x1,
            ConfigurationDescriptorType = 0x2,
            StringDescriptorType = 0x3,
            InterfaceDescriptorType = 0x4,
            EndpointDescriptorType = 0x5,
            Hub20Descriptor = 0x29,
            Hub30Descriptor = 0x2A
        }

        public enum UsbConfiguration : byte
        {
            RemoteWakeUp = 32,
            SelfPowered = 64,
            BusPowered = 128,
            RemoteWakeUpBusPowered = 160,
            RemoteWakeUpSelfPowered = 96
        }

        public enum UsbTransfer : byte
        {
            Control = 0x0,
            Isochronous = 0x1,
            Bulk = 0x2,
            Interrupt = 0x3
        }

        public enum UsbConnectionStatus
        {
            NoDeviceConnected,
            DeviceConnected,
            DeviceFailedEnumeration,
            DeviceGeneralFailure,
            DeviceCausedOvercurrent,
            DeviceNotEnoughPower,
            DeviceNotEnoughBandwidth,
            DeviceHubNestedTooDeeply,
            DeviceInLegacyHub,
            DeviceEnumerating,
            DeviceReset
        }

        public enum UsbDeviceSpeed : byte
        {
            UsbLowSpeed,
            UsbFullSpeed,
            UsbHighSpeed,
            UsbSuperSpeed
        }

        public enum DeviceInterfaceDataFlags : uint
        {
            Unknown = 0x00000000,
            Active = 0x00000001,
            Default = 0x00000002,
            Removed = 0x00000004
        }

        public enum HubPortStatus : short
        {
            Connection = 0x0001,
            Enabled = 0x0002,
            Suspend = 0x0004,
            OverCurrent = 0x0008,
            BeingReset = 0x0010,
            Power = 0x0100,
            LowSpeed = 0x0200,
            HighSpeed = 0x0400,
            TestMode = 0x0800,
            Indicator = 0x1000,
            // these are the bits which cause the hub port state machine to keep moving 
            //kHubPortStateChangeMask = kHubPortConnection | kHubPortEnabled | kHubPortSuspend | kHubPortOverCurrent | kHubPortBeingReset 
        }

        public enum HubStatus : byte
        {
            LocalPowerStatus = 1,
            OverCurrentIndicator = 2,
            LocalPowerStatusChange = 1,
            OverCurrentIndicatorChange = 2
        }

        public enum PortIndicatorSlectors : byte
        {
            IndicatorAutomatic = 0,
            IndicatorAmber,
            IndicatorGreen,
            IndicatorOff
        }

        public enum PowerSwitching : byte
        {
            SupportsGangPower = 0,
            SupportsIndividualPortPower = 1,
            SetPowerOff = 0,
            SetPowerOn = 1
        }

        /// <summary>
        /// Device registry property codes
        /// </summary>
        public enum Spdrp
        {
            /// <summary>
            /// DeviceDesc (R/W)
            /// </summary>
            SpdrpDevicedesc = 0x00000000,

            /// <summary>
            /// HardwareID (R/W)
            /// </summary>
            SpdrpHardwareid = 0x00000001,

            /// <summary>
            /// CompatibleIDs (R/W)
            /// </summary>
            SpdrpCompatibleids = 0x00000002,

            /// <summary>
            /// unused
            /// </summary>
            SpdrpUnused0 = 0x00000003,

            /// <summary>
            /// Service (R/W)
            /// </summary>
            SpdrpService = 0x00000004,

            /// <summary>
            /// unused
            /// </summary>
            SpdrpUnused1 = 0x00000005,

            /// <summary>
            /// unused
            /// </summary>
            SpdrpUnused2 = 0x00000006,

            /// <summary>
            /// Class (R--tied to ClassGUID)
            /// </summary>
            SpdrpClass = 0x00000007,

            /// <summary>
            /// ClassGUID (R/W)
            /// </summary>
            SpdrpClassguid = 0x00000008,

            /// <summary>
            /// Driver (R/W)
            /// </summary>
            SpdrpDriver = 0x00000009,

            /// <summary>
            /// ConfigFlags (R/W)
            /// </summary>
            SpdrpConfigflags = 0x0000000A,

            /// <summary>
            /// Mfg (R/W)
            /// </summary>
            SpdrpMfg = 0x0000000B,

            /// <summary>
            /// FriendlyName (R/W)
            /// </summary>
            SpdrpFriendlyname = 0x0000000C,

            /// <summary>
            /// LocationInformation (R/W)
            /// </summary>
            SpdrpLocationInformation = 0x0000000D,

            /// <summary>
            /// PhysicalDeviceObjectName (R)
            /// </summary>
            SpdrpPhysicalDeviceObjectName = 0x0000000E,

            /// <summary>
            /// Capabilities (R)
            /// </summary>
            SpdrpCapabilities = 0x0000000F,

            /// <summary>
            /// UiNumber (R)
            /// </summary>
            SpdrpUiNumber = 0x00000010,

            /// <summary>
            /// UpperFilters (R/W)
            /// </summary>
            SpdrpUpperfilters = 0x00000011,

            /// <summary>
            /// LowerFilters (R/W)
            /// </summary>
            SpdrpLowerfilters = 0x00000012,

            /// <summary>
            /// BusTypeGUID (R)
            /// </summary>
            SpdrpBustypeguid = 0x00000013,

            /// <summary>
            /// LegacyBusType (R)
            /// </summary>
            SpdrpLegacybustype = 0x00000014,

            /// <summary>
            /// BusNumber (R)
            /// </summary>
            SpdrpBusnumber = 0x00000015,

            /// <summary>
            /// Enumerator Name (R)
            /// </summary>
            SpdrpEnumeratorName = 0x00000016,

            /// <summary>
            /// Security (R/W, binary form)
            /// </summary>
            SpdrpSecurity = 0x00000017,

            /// <summary>
            /// Security (W, SDS form)
            /// </summary>
            SpdrpSecuritySds = 0x00000018,

            /// <summary>
            /// Device Type (R/W)
            /// </summary>
            SpdrpDevtype = 0x00000019,

            /// <summary>
            /// Device is exclusive-access (R/W)
            /// </summary>
            SpdrpExclusive = 0x0000001A,

            /// <summary>
            /// Device Characteristics (R/W)
            /// </summary>
            SpdrpCharacteristics = 0x0000001B,

            /// <summary>
            /// Device Address (R)
            /// </summary>
            SpdrpAddress = 0x0000001C,

            /// <summary>
            /// UiNumberDescFormat (R/W)
            /// </summary>
            SpdrpUiNumberDescFormat = 0X0000001D,

            /// <summary>
            /// Device Power Data (R)
            /// </summary>
            SpdrpDevicePowerData = 0x0000001E,

            /// <summary>
            /// Removal Policy (R)
            /// </summary>
            SpdrpRemovalPolicy = 0x0000001F,

            /// <summary>
            /// Hardware Removal Policy (R)
            /// </summary>
            SpdrpRemovalPolicyHwDefault = 0x00000020,

            /// <summary>
            /// Removal Policy Override (RW)
            /// </summary>
            SpdrpRemovalPolicyOverride = 0x00000021,

            /// <summary>
            /// Device Install State (R)
            /// </summary>
            SpdrpInstallState = 0x00000022,

            /// <summary>
            /// Device Location Paths (R)
            /// </summary>
            SpdrpLocationPaths = 0x00000023,
        }

        #endregion

        // *******************************************************************************************
        // *************************************** stuctures *****************************************
        // *******************************************************************************************

        #region structures

        [StructLayout(LayoutKind.Sequential)]
        public struct SpClassinstallHeader
        {
            public int CbSize;
            public int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SpPropchangeParams
        {
            public SpClassinstallHeader ClassInstallHeader;
            public int StateChange;
            public int Scope;
            public int HwProfile;

            public void Init()
            {
                ClassInstallHeader = new SpClassinstallHeader();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SpDevinfoData
        {
            public int CbSize;
            public Guid ClassGuid;
            public Int32 DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SpDeviceInterfaceData
        {
            public int CbSize;
            public Guid InterfaceClassGuid;
            public DeviceInterfaceDataFlags Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SpDeviceInterfaceDetailData
        {
            public int CbSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbHcdDriverkeyName
        {
            public int ActualLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string DriverKeyName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbRootHubName
        {
            public int ActualLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string RootHubName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbHubDescriptor
        {
            public byte DescriptorLength;
            public UsbDescriptorType DescriptorType;
            public byte NumberOfPorts;
            public short HubCharacteristics;
            public byte PowerOnToPowerGood;
            public byte HubControlCurrent;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] RemoveAndPowerMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Usb30HubDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public byte NumberOfPorts;
            public ushort HubCharacteristics;
            public byte PowerOnToPowerGood;
            public byte HubControlCurrent;
            public byte HubHdrDecLat;
            public ushort HubDelay;
            public ushort DeviceRemovable;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbHubInformation
        {
            public UsbHubDescriptor HubDescriptor;
            public bool HubIsBusPowered;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbNodeInformation
        {
            public UsbHubNode NodeType;
            public UsbHubInformation HubInformation;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbHubCapabilitiesEx
        {
            public UsbHubCapFlags CapabilityFlags;
        }

        [Flags]
        public enum UsbHubCapFlags
        {
            HubIsHighSpeedCapable = 0,
            HubIsHighSpeed = 1 << 0,
            HubIsMultiTtCapable = 1 << 1,
            HubIsMultiTt = 1 << 2,
            HubIsRoot = 1 << 3,
            HubIsArmedWakeOnConnect = 1 << 4,
            HubIsBusPowered = 1 << 5
        }

        public struct UsbHubInformationEx : IMarshallable
        {
            public UsbHubType HubType;
            public ushort HighestPortNumber;
            public UsbHubDescriptor UsbHubDescriptor;
            public Usb30HubDescriptor Usb30HubDescriptor;

            public int SizeOf => sizeof(UsbHubType) + sizeof(ushort) +
                                 Math.Max(Marshal.SizeOf(UsbHubDescriptor), Marshal.SizeOf(Usb30HubDescriptor));

            public void MarshalFrom(IntPtr pointer)
            {
                HubType = (UsbHubType) Marshal.ReadInt32(pointer);
                HighestPortNumber = (ushort) Marshal.ReadInt16(pointer, sizeof(UsbHubType));

                IntPtr ptr = new IntPtr(pointer.ToInt64() + 6);

                UsbHubDescriptor = (UsbHubDescriptor) Marshal.PtrToStructure(ptr, typeof(UsbHubDescriptor));
                Usb30HubDescriptor = (Usb30HubDescriptor)Marshal.PtrToStructure(ptr, typeof(Usb30HubDescriptor));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsbNodeConnectionInformationEx
        {
            public int ConnectionIndex;
            public UsbDeviceDescriptor DeviceDescriptor;
            public byte CurrentConfigurationValue;
            public UsbDeviceSpeed Speed;
            public byte DeviceIsHub;
            public short DeviceAddress;
            public int NumberOfOpenPipes;
            public UsbConnectionStatus ConnectionStatus;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class UsbDeviceDescriptor
        {
            public byte Length;
            public UsbApi.UsbDescriptorType DescriptorType;
            public short bcdUSB;
            public UsbApi.UsbDeviceClass bDeviceClass;
            public byte DeviceSubClass;
            public byte DeviceProtocol;
            public byte MaxPacketSize0;
            public ushort IdVendor;
            public ushort IdProduct;
            public short bcdDevice;
            public byte IManufacturer;
            public byte IProduct;
            public byte ISerialNumber;
            public byte NumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbEndpointDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public byte EndpointAddress;
            public UsbTransfer Attributes;
            public short MaxPacketSize;
            public byte Interval;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbStringDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaximumUsbStringLength)]
            public string String;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbSetupPacket
        {
            public byte BmRequest;
            public byte BRequest;
            public short Value;
            public short Index;
            public short Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbDescriptorRequest
        {
            public int ConnectionIndex;
            public UsbSetupPacket SetupPacket;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbNodeConnectionName
        {
            public int ConnectionIndex;
            public int ActualLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string NodeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbNodeConnectionDriverkeyName // Yes, this is the same as the structure above...
        {
            public int ConnectionIndex;
            public int ActualLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxBufferSize)]
            public string DriverKeyName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct StorageDeviceNumber
        {
            public int DeviceType;
            public int DeviceNumber;
            public int PartitionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbInterfaceDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public byte InterfaceNumber;
            public byte AlternateSetting;
            public byte NumEndpoints;
            public byte InterfaceClass;
            public byte InterfaceSubClass;
            public byte InterfaceProtocol;
            public byte Interface;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UsbConfigurationDescriptor
        {
            public byte Length;
            public UsbDescriptorType DescriptorType;
            public short TotalLength;
            public byte NumInterface;
            public byte ConfigurationsValue;
            public byte IConfiguration;
            public UsbConfiguration Attributes;
            public byte MaxPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SpDevinfoData1
        {
            public int CbSize;
            public Guid ClassGuid;
            public int DevInst;
            public ulong Reserved;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class RawRootportParameters
        {
            public ushort PortNumber;
            public ushort PortStatus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class UsbUnicodeName
        {
            public ulong Length;
            public string Str;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct UsbPortConnectorProperties
        {
            public uint ConnectionIndex;
            public uint ActualLength;
            public uint Properties;
            public ushort CompanionIndex;
            public ushort CompanionPortNumber;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string CompanionHubSymbolicLinkName;
        }

        [Flags]
        public enum UsbPortProperties : ulong
        {
            PortIsUserConnectable = 0 << 0,
            PortIsDebugCapable = 1 << 0,
            PortHasMultipleCompanions = 1 << 1,
            PortConnectorIsTypeC = 1 << 2
        }

    // HID.DLL definitions

    #endregion

    // *******************************************************************************************
    // *************************************** methodes ******************************************
    // *******************************************************************************************

    #region methodes

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, int enumerator, IntPtr hwndParent,
            int flags); // 1st form using a ClassGUID

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(int classGuid, string enumerator, IntPtr hwndParent,
            int flags); // 2nd form uses an Enumerator

        [DllImport("setupapi.dll")]
        internal static extern IntPtr SetupDiGetClassDevsEx(IntPtr classGuid,
            [MarshalAs(UnmanagedType.LPStr)] String enumerator, IntPtr hwndParent, Int32 flags, IntPtr deviceInfoSet,
            [MarshalAs(UnmanagedType.LPStr)] String machineName, IntPtr reserved);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
            ref Guid interfaceClassGuid, int memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
            ref SpDeviceInterfaceData deviceInterfaceData, ref SpDeviceInterfaceDetailData deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize, ref int requiredSize, ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(IntPtr deviceInfoSet,
            ref SpDevinfoData deviceInfoData, int iProperty, ref int propertyRegDataType, IntPtr propertyBuffer,
            int propertyBufferSize, ref int requiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, int memberIndex,
            ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInstanceId(IntPtr deviceInfoSet, ref SpDevinfoData deviceInfoData,
            StringBuilder deviceInstanceId, int deviceInstanceIdSize, out int requiredSize);

        // CreateFile() returning SafeHandle is better for use with HidD_xxxx methods

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SpDevinfoData deviceInfoData,
            ref SpClassinstallHeader classInstallParams, int classInstallParamsSize);

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiCallClassInstaller(int installFunction, IntPtr deviceInfoSet,
            ref SpDevinfoData deviceInfoData);

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiClassGuidsFromNameA(string classN, ref Guid guids, UInt32 classNameSize,
            ref UInt32 reqSize);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, uint iEnumerator, int hwndParent,
            int flags);

        [DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInfo(IntPtr lpInfoSet, UInt32 dwIndex, SpDevinfoData1 devInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(IntPtr lpInfoSet, SpDevinfoData1 deviceInfoData,
            UInt32 property, UInt32 propertyRegDataType, StringBuilder propertyBuffer, UInt32 propertyBufferSize,
            IntPtr requiredSize);

        [DllImport("quickusb.dll", CharSet = CharSet.Ansi)]
        internal static extern int QuickUsbOpen(out IntPtr handle, string devName);

        // Hid.dll definitions
        // Length for use with HID Api functions

        #endregion

        #endregion
    }
}
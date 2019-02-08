using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    /// static class to factory class to build the connected devices
    /// </summary>
    public static class DeviceFactory
    {
        /// <summary>
        /// Builds the device.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="portCount">The port count.</param>
        /// <param name="devicePath">The device path.</param>
        /// <returns>The device.</returns>
        public static Device BuildDevice(Device parent, uint portCount, string devicePath)
        {
            Device device = null;

            // Open a handle to the Hub device
            IntPtr deviceHandle = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite,
                IntPtr.Zero,
                UsbApi.OpenExisting, 0, IntPtr.Zero);

            try
            {
                if (deviceHandle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    if (GetNodeConnectionInformationEx(portCount, deviceHandle, out var nodeConnection))
                    {
                        if (nodeConnection.ConnectionStatus == UsbIoControl.UsbConnectionStatus.DeviceConnected)
                        {
                            GetUsbPortConnectorProperties(portCount, deviceHandle,
                                out var portConnectorProperties);

                            GetNodeConnectionInformationExV2(portCount, deviceHandle, out var nodeConnectionV2);

                            if (nodeConnection.DeviceDescriptor.bDeviceClass == UsbDesc.DeviceClassType.UsbHubDevice)
                            {
                                if (GetUsbNodeConnectionName(deviceHandle, out var connectionName))
                                {
                                    string name = @"\\?\" + connectionName.NodeName;

                                    device = new UsbHub(parent, nodeConnection.DeviceDescriptor, name)
                                    {
                                        NodeConnectionInfo = nodeConnection,
                                        NodeConnectionInfoV2 = nodeConnectionV2,
                                        UsbPortConnectorProperties = portConnectorProperties
                                    };
                                }
                            }
                            else
                            {
                                device = new UsbDevice(parent, nodeConnection.DeviceDescriptor, portCount, devicePath)
                                {
                                    NodeConnectionInfo = nodeConnection,
                                    NodeConnectionInfoV2 = nodeConnectionV2,
                                    UsbPortConnectorProperties = portConnectorProperties
                                };
                            }
                        }
                        else
                        {
                            device = new UsbDevice(parent, null, portCount) {NodeConnectionInfo = nodeConnection};
                        }
                    }
                }
            }
            finally
            {
                if (deviceHandle.ToInt64() != UsbApi.InvalidHandleValue)
                    KernelApi.CloseHandle(deviceHandle);
            }

            return device;
        }

        private static void GetNodeConnectionInformationExV2(uint portCount, IntPtr deviceHandle,
            out UsbIoControl.UsbNodeConnectionInformationExV2 usbNodeConnectionInformationExV2)
        {
            IntPtr structPtr = IntPtr.Zero;

            try
            {
                int bytesRequested = Marshal.SizeOf(typeof(UsbIoControl.UsbNodeConnectionInformationExV2));

                structPtr = Marshal.AllocHGlobal(bytesRequested);
                usbNodeConnectionInformationExV2 =
                    new UsbIoControl.UsbNodeConnectionInformationExV2
                    {
                        ConnectionIndex = portCount,
                        SupportedUsbProtocols = UsbIoControl.UsbProtocols.Usb300,
                        Length = (uint) bytesRequested
                    };

                Marshal.StructureToPtr(usbNodeConnectionInformationExV2, structPtr, true);

                if (KernelApi.DeviceIoControl(deviceHandle, UsbIoControl.IoctlUsbGetNodeConnectionInformationExV2,
                    structPtr, bytesRequested, structPtr, bytesRequested, out _, IntPtr.Zero))
                {
                    usbNodeConnectionInformationExV2 =
                        (UsbIoControl.UsbNodeConnectionInformationExV2)Marshal.PtrToStructure(structPtr,
                            typeof(UsbIoControl.UsbNodeConnectionInformationExV2));
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetNodeConnectionInformationExV2)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(structPtr);
                }
            }
        }

        private static void GetUsbPortConnectorProperties(uint portCount, IntPtr deviceHandle,
            out UsbIoControl.UsbPortConnectorProperties usbPortConnectorProperties)
        {
            IntPtr structPtr = IntPtr.Zero;

            try
            {
                int bytesRequested = Marshal.SizeOf(typeof(UsbIoControl.UsbPortConnectorProperties));

                structPtr = Marshal.AllocHGlobal(bytesRequested);
                usbPortConnectorProperties =
                    new UsbIoControl.UsbPortConnectorProperties() {ConnectionIndex = portCount};
                Marshal.StructureToPtr(usbPortConnectorProperties, structPtr, true);

                if (KernelApi.DeviceIoControl(deviceHandle, UsbIoControl.IoctlUsbGetPortConnectorProperties,
                    structPtr, bytesRequested, structPtr, bytesRequested, out _, IntPtr.Zero))
                {
                    usbPortConnectorProperties =
                        (UsbIoControl.UsbPortConnectorProperties) Marshal.PtrToStructure(structPtr,
                            typeof(UsbIoControl.UsbPortConnectorProperties));
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetPortConnectorProperties)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(structPtr);
                }
            }
        }

        private static bool GetNodeConnectionInformationEx(uint portCount, IntPtr deviceHandle,
            out UsbIoControl.UsbNodeConnectionInformationEx nodeConnection)
        {
            IntPtr structPtr = IntPtr.Zero;

            try
            {
                int bytesRequested = Marshal.SizeOf(typeof(UsbIoControl.UsbNodeConnectionInformationEx));

                structPtr = Marshal.AllocHGlobal(bytesRequested);
                nodeConnection = new UsbIoControl.UsbNodeConnectionInformationEx {ConnectionIndex = portCount};
                Marshal.StructureToPtr(nodeConnection, structPtr, true);

                if (KernelApi.DeviceIoControl(deviceHandle, UsbIoControl.IoctlUsbGetNodeConnectionInformationEx,
                    structPtr, bytesRequested, structPtr, bytesRequested, out _, IntPtr.Zero))
                {
                    nodeConnection =
                        (UsbIoControl.UsbNodeConnectionInformationEx) Marshal.PtrToStructure(structPtr,
                            typeof(UsbIoControl.UsbNodeConnectionInformationEx));
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetNodeConnectionInformationEx)}] Result: [{KernelApi.GetLastError():X}]");

                    return false;
                }
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(structPtr);
                }
            }

            return true;
        }

        private static bool GetUsbNodeConnectionName(IntPtr deviceHandle,
            out UsbIoControl.UsbNodeConnectionName nodeConnectionName)
        {
            IntPtr structPtr = IntPtr.Zero;

            try
            {
                int bytesRequested = Marshal.SizeOf(typeof(UsbIoControl.UsbNodeConnectionName));

                structPtr = Marshal.AllocHGlobal(bytesRequested);
                nodeConnectionName = new UsbIoControl.UsbNodeConnectionName();
                Marshal.StructureToPtr(nodeConnectionName, structPtr, true);

                if (KernelApi.DeviceIoControl(deviceHandle, UsbIoControl.IoctlUsbGetNodeConnectionName,
                    structPtr, bytesRequested, structPtr, bytesRequested, out _, IntPtr.Zero))
                {
                    nodeConnectionName =
                        (UsbIoControl.UsbNodeConnectionName) Marshal.PtrToStructure(structPtr,
                            typeof(UsbIoControl.UsbNodeConnectionName));

                    return false;
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetNodeConnectionName)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(structPtr);
                }
            }

            return true;
        }
    }
}
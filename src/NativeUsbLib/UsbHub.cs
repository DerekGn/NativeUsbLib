using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeUsbLib.Exceptions;
using NativeUsbLib.WinApis;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb hub
    /// </summary>
    public class UsbHub : Device
    {
        /// <summary>
        /// Gets the port count.
        /// </summary>
        /// <value>The port count.</value>
        public int PortCount => NodeInformation.HubInformation.HubDescriptor.NumberOfPorts;

        /// <summary>
        /// Gets a value indicating whether this instance is bus powered.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is bus powered; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusPowered => Convert.ToBoolean(NodeInformation.HubInformation.HubIsBusPowered);

        /// <summary>
        /// Gets a value indicating whether this instance is root hub.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is root hub; otherwise, <c>false</c>.
        /// </value>
        public bool IsRootHub { get; private set; }

        /// <summary>
        /// Gets the node information.
        /// </summary>
        /// <value>The node information.</value>
        public UsbApi.UsbNodeInformation NodeInformation { get; protected set; }

        /// <summary>
        /// Gets the usb hub information.
        /// </summary>
        /// <value>The usb hub information.</value>
        public UsbIoControl.UsbHubInformationEx HubInformation { get; protected set; }

        /// <summary>
        /// Gets the hub capabilities
        /// </summary>
        public UsbApi.UsbHubCapabilitiesEx UsbHubCapabilitiesEx { get; protected set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UsbHub"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbHub(UsbSpec.UsbDeviceDescriptor deviceDescriptor, string devicePath)
            : base(deviceDescriptor, 0, devicePath)
        {
            DeviceDescription = "Standard-USB-Hub";
            DevicePath = devicePath;

            IntPtr hostControllerHandle = IntPtr.Zero;

            try
            {
                hostControllerHandle = KernelApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);

                if (hostControllerHandle.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    GetRootHubName(hostControllerHandle);

                    // TODO: Get the driver key name for the root hub.

                    IntPtr hubHandle = IntPtr.Zero;
                    
                    try
                    {
                        // Now let's open the hub (based upon the hub name we got above).
                        hubHandle = KernelApi.CreateFile(DevicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);

                        if (hubHandle.ToInt64() != UsbApi.InvalidHandleValue)
                        {
                            GetUsbNodeInformation(hubHandle);

                            GetUsbHubInformation(hubHandle);

                            GetHubCapabilities(hubHandle);
                        }
                    }
                    finally
                    {
                        if(hubHandle != IntPtr.Zero)
                            KernelApi.CloseHandle(hubHandle);
                    }
                }
                else
                    throw new UsbHubException("No port found!");
            }
            finally
            {
                if(hostControllerHandle != IntPtr.Zero)
                    KernelApi.CloseHandle(hostControllerHandle);
            }

            for (uint index = 1; index <= PortCount; index++)
            {
                // Initialize a new port and save the port.
                try
                {
                    Devices.Add(DeviceFactory.BuildDevice(this, index, DevicePath));
                }
                catch (Exception e)
                {
                    Trace.TraceError("Unhandled exception occurred: {0}", e);
                }
            }
        }

        private void GetHubCapabilities(IntPtr hubHandle)
        {
            UsbApi.UsbHubCapabilitiesEx usbHubCapabilitiesEx = new UsbApi.UsbHubCapabilitiesEx();
            int nBytes = Marshal.SizeOf(usbHubCapabilitiesEx);
            IntPtr ptrUsbHubCapabilitiesEx = IntPtr.Zero;

            try
            {
                ptrUsbHubCapabilitiesEx = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(usbHubCapabilitiesEx, ptrUsbHubCapabilitiesEx, true);

                if (KernelApi.DeviceIoControl(hubHandle, UsbIoControl.IoctlUsbGetHubCapabilitiesEx, ptrUsbHubCapabilitiesEx, nBytes,
                    ptrUsbHubCapabilitiesEx, nBytes, out _, IntPtr.Zero))
                {
                    UsbHubCapabilitiesEx =
                        (UsbApi.UsbHubCapabilitiesEx)Marshal.PtrToStructure(ptrUsbHubCapabilitiesEx,
                            typeof(UsbApi.UsbHubCapabilitiesEx));
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetHubCapabilitiesEx)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if(ptrUsbHubCapabilitiesEx != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrUsbHubCapabilitiesEx);
            }
        }

        private void GetUsbHubInformation(IntPtr hubHandle)
        {
            UsbIoControl.UsbHubInformationEx hubInfoEx =
                new UsbIoControl.UsbHubInformationEx();
            int nBytes = hubInfoEx.SizeOf;
            IntPtr ptrHubInfo = IntPtr.Zero;

            try
            {
                ptrHubInfo = Marshal.AllocHGlobal(nBytes);

                if (KernelApi.DeviceIoControl(hubHandle, UsbIoControl.IoctlUsbGetHubInformationEx, ptrHubInfo, nBytes, ptrHubInfo,
                    nBytes, out _, IntPtr.Zero))
                {
                    hubInfoEx.MarshalFrom(ptrHubInfo);
                    HubInformation = hubInfoEx;
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetNodeInformation)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if(ptrHubInfo != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrHubInfo);
            }
        }

        private void GetUsbNodeInformation(IntPtr hubHandle)
        {
            UsbApi.UsbNodeInformation nodeInfo =
                new UsbApi.UsbNodeInformation {NodeType = UsbApi.UsbHubNode.UsbHub};
            int nBytes = Marshal.SizeOf(nodeInfo);
            IntPtr ptrNodeInfo = IntPtr.Zero;

            try
            {
                ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                Marshal.StructureToPtr(nodeInfo, ptrNodeInfo, true);

                if (KernelApi.DeviceIoControl(hubHandle, UsbIoControl.IoctlUsbGetNodeInformation, ptrNodeInfo, nBytes, ptrNodeInfo,
                    nBytes, out _, IntPtr.Zero))
                {
                    NodeInformation =
                        (UsbApi.UsbNodeInformation)Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.UsbNodeInformation));
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetNodeInformation)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (ptrNodeInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrNodeInfo);
                }
            }
        }

        private void GetRootHubName(IntPtr hostControllerHandle)
        {
            UsbApi.UsbRootHubName rootHubName = new UsbApi.UsbRootHubName();
            int nBytes = Marshal.SizeOf(rootHubName);
            IntPtr ptrRootHubName = IntPtr.Zero;

            try
            {
                ptrRootHubName = Marshal.AllocHGlobal(nBytes);

                // Get the root hub name.
                if (KernelApi.DeviceIoControl(hostControllerHandle, UsbIoControl.IoctlUsbGetRootHubName, ptrRootHubName, nBytes,
                    ptrRootHubName, nBytes, out _, IntPtr.Zero))
                {
                    rootHubName = (UsbApi.UsbRootHubName)Marshal.PtrToStructure(ptrRootHubName, typeof(UsbApi.UsbRootHubName));

                    if (rootHubName.ActualLength > 0)
                    {
                        IsRootHub = true;
                        DeviceDescription = "RootHub";
                        DevicePath = @"\\?\" + rootHubName.RootHubName;
                    }
                }
                else
                {
                    Trace.TraceError(
                        $"[{nameof(KernelApi.DeviceIoControl)}] [{nameof(UsbIoControl.IoctlUsbGetRootHubName)}] Result: [{KernelApi.GetLastError():X}]");
                }
            }
            finally
            {
                if (ptrRootHubName != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrRootHubName);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NativeUsbLib.Exceptions;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb hub
    /// </summary>
    public class UsbHub : Device
    {
        #region fields

        /// <summary>
        /// Gets the port count.
        /// </summary>
        /// <value>The port count.</value>
        public int PortCount { get; private set; } = -1;

        /// <summary>
        /// Gets a value indicating whether this instance is bus powered.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is bus powered; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusPowered { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether this instance is root hub.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is root hub; otherwise, <c>false</c>.
        /// </value>
        public bool IsRootHub { get; } = false;

        private UsbApi.UsbNodeInformation m_NodeInformation;
        
        /// <summary>
        /// Gets or sets the node information.
        /// </summary>
        /// <value>The node information.</value>
        public UsbApi.UsbNodeInformation NodeInformation
        {
            get { return m_NodeInformation; }
            set
            {
                m_NodeInformation = value;
                IsBusPowered = Convert.ToBoolean(m_NodeInformation.HubInformation.HubIsBusPowered);
                PortCount = m_NodeInformation.HubInformation.HubDescriptor.BNumberOfPorts;
            }
        }

        /// <summary>
        /// Gets or sets the node information.
        /// </summary>
        /// <value>The node information.</value>
        public UsbHubInformation NodeInformationX { get; protected set; }

        /// <summary>
        /// Gets the port count.
        /// </summary>
        /// <value>The port count.</value>
        //public int PortCountX
        //{
        //    get
        //    {
        //        //NodeInformationX.
        //    };
        //}

        /// <summary>
        /// Gets a value indicating whether this instance is bus powered.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is bus powered; otherwise, <c>false</c>.
        /// </value>
        //public bool IsBusPoweredX
        //{
        //    get
        //    {
        //        return 
        //    };
        //}

        #endregion

        #region constructor/destructor

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbHub"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="deviceDescriptor">The device descriptor.</param>
        /// <param name="devicePath">The device path.</param>
        public UsbHub(Device parent, UsbApi.UsbDeviceDescriptor deviceDescriptor, string devicePath)
            : base(parent, deviceDescriptor, -1, devicePath)
        {
            DeviceDescription = "Standard-USB-Hub";
            DevicePath = devicePath;

            int nBytesReturned;

            // Open a handle to the host controller.
            IntPtr handel1 = UsbApi.CreateFile(devicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);
            if (handel1.ToInt64() != UsbApi.InvalidHandleValue)
            {

                UsbApi.UsbRootHubName rootHubName = new UsbApi.UsbRootHubName();
                int nBytes = Marshal.SizeOf(rootHubName);
                IntPtr ptrRootHubName = Marshal.AllocHGlobal(nBytes);

                // Get the root hub name.
                if (UsbApi.DeviceIoControl(handel1, UsbApi.IoctlUsbGetRootHubName, ptrRootHubName, nBytes, ptrRootHubName, nBytes, out nBytesReturned, IntPtr.Zero))
                {
                    rootHubName = (UsbApi.UsbRootHubName)Marshal.PtrToStructure(ptrRootHubName, typeof(UsbApi.UsbRootHubName));

                    if (rootHubName.ActualLength > 0)
                    {
                        IsRootHub = true;
                        DeviceDescription = "RootHub";
                        DevicePath = @"\\?\" + rootHubName.RootHubName;
                    }
                }

                Marshal.FreeHGlobal(ptrRootHubName);

                // TODO: Get the driver key name for the root hub.

                // Now let's open the hub (based upon the hub name we got above).
                IntPtr handel2 = UsbApi.CreateFile(DevicePath, UsbApi.GenericWrite, UsbApi.FileShareWrite, IntPtr.Zero, UsbApi.OpenExisting, 0, IntPtr.Zero);
                if (handel2.ToInt64() != UsbApi.InvalidHandleValue)
                {
                    OperatingSystem osVersionInfo = Environment.OSVersion;
                    
                    if (osVersionInfo.Version.Major >= 6 && osVersionInfo.Version.Minor >= 2)
                    {
                        //UsbApi.UsbNodeInformationEx nodeInfo =
                        //    new UsbApi.UsbNodeInformationEx { NodeType = UsbApi.UsbHubNode.UsbHub };
                        //nBytes = Marshal.SizeOf(nodeInfo);
                        //IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                        //Marshal.StructureToPtr(nodeInfo, ptrNodeInfo, true);

                        //if (UsbApi.DeviceIoControl(handel2, UsbApi.IoctlUsbGetNodeInformationEx, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                        //{
                        //    NodeInformationX = new UsbThreeNodeInformation((UsbApi.UsbNodeInformationEx)Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.UsbNodeInformationEx)));
                        //}
                        //Marshal.FreeHGlobal(ptrNodeInfo);
                    }
                    else
                    {
                        UsbApi.UsbNodeInformation nodeInfo =
                            new UsbApi.UsbNodeInformation { NodeType = UsbApi.UsbHubNode.UsbHub };
                        nBytes = Marshal.SizeOf(nodeInfo);
                        IntPtr ptrNodeInfo = Marshal.AllocHGlobal(nBytes);
                        Marshal.StructureToPtr(nodeInfo, ptrNodeInfo, true);

                        if (UsbApi.DeviceIoControl(handel2, UsbApi.IoctlUsbGetNodeInformation, ptrNodeInfo, nBytes, ptrNodeInfo, nBytes, out nBytesReturned, IntPtr.Zero))
                        {
                            NodeInformationX = new UsbTwoHubInformation((UsbApi.UsbNodeInformation)Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.UsbNodeInformation)));
                            NodeInformation = (UsbApi.UsbNodeInformation) Marshal.PtrToStructure(ptrNodeInfo, typeof(UsbApi.UsbNodeInformation));
                        }

                        Marshal.FreeHGlobal(ptrNodeInfo);
                    }

                    UsbApi.CloseHandle(handel2);
                }

                UsbApi.CloseHandle(handel1);

                for (int index = 1; index <= PortCount; index++)
                {

                    // Initialize a new port and save the port.
                    try
                    {
                        Devices.Add(DeviceFactory.BuildDevice(this, index, DevicePath));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Unhandled exception occurred: {0}", e.ToString());
                    }
                }
            }
            else
                throw new UsbHubException("No port found!");
        }

        #endregion

        #endregion
    }
}
/*
This assembly is based on the UsbViewer in C# project at SourceForge.net
The author appears to be Nick Vetter
Copyright (C) 2013 Nick Vetter

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NativeUsbLib
{
    /// <summary>
    /// Usb bus
    /// </summary>
    public class UsbBus : Device
    {
        #region constructor/destructor

        #region constructor


        /// <summary>
        /// Initializes a new instance of the <see cref="UsbBus"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public UsbBus(Device parent):base(parent, null, 0, null)
        {
            ScanBus();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbBus"/> class.
        /// </summary>
        public UsbBus():this(null)
        {
        }

        #endregion


        #endregion

        #region methodes

        #region ScanBus

        private bool ScanBus()
        {
            bool success = true;
            for (uint index = 0; success; index++)
            {

                // Initialize a new controller and save the index of the controller.
                try
                {
                    Devices.Add(new UsbController(this, index));
                }
                catch
                {
                    success = false;
                }
            }

            if (Devices.Count >= 0)
                return true;

            return false;
        }

        #endregion

        #region methode Refresh

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        /// <returns></returns>
        public bool Refresh()
        {
            Devices.Clear();
            return ScanBus();
        }

        #endregion

        private void ScanHubs(ushort vendorid, ushort productid, string serial, UsbHub hub, ref List<UsbDevice> devices)
        {
            foreach (Device device in hub.ChildDevices)
            {
                if (device.IsHub)
                    ScanHubs(vendorid, productid, serial, device as UsbHub, ref devices);

                if (device.DeviceDescriptor != null && serial != null)
                    if (device.DeviceDescriptor.idVendor == vendorid && device.DeviceDescriptor.idProduct == productid && device.SerialNumber.Equals(serial))
                        devices.Add(device as UsbDevice);

                if (device.DeviceDescriptor != null && serial == null)
                    if (device.DeviceDescriptor.idVendor == vendorid && device.DeviceDescriptor.idProduct == productid)
                        devices.Add(device as UsbDevice);
            }
        }

        public List<UsbDevice> GetDeviceByVidPidSerialNumber(ushort vendorid, ushort productid, string serial)
        {
            List<UsbDevice> devices = new List<UsbDevice>();
            foreach (UsbController controller in Controller)
                foreach (UsbHub hub in controller.Hubs)
                    ScanHubs(vendorid, productid, serial, hub, ref devices);

            return devices;
        }

        public List<UsbDevice> GetDeviceByVidPid(ushort vendorid, ushort productid)
        {
            List<UsbDevice> devices = new List<UsbDevice>();
            foreach (UsbController controller in Controller)
                foreach (UsbHub hub in controller.Hubs)
                    ScanHubs(vendorid, productid, null, hub, ref devices);

            return devices;
        }

       #endregion

        #region properties

        #region Controller

        /// <summary>
        /// Gets the controller.
        /// </summary>
        /// <value>The controller.</value>
        public ReadOnlyCollection<UsbController> Controller
        {
            get
            {
                UsbController[] controllers = new UsbController[Devices.Count];
                Devices.CopyTo(controllers);
                return new ReadOnlyCollection<UsbController>(controllers);
            }
        }

        #endregion

        #endregion
    }
}
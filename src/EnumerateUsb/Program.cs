using NativeUsbLib;
using System;

namespace EnumerateUsb
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                UsbBus usbBus = new UsbBus();

                foreach (UsbController controller in usbBus.Controller)
                    ShowController(controller);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ShowController(UsbController controller)
        {
            if (controller != null)
            {
                Console.WriteLine($"Controller: [{controller.DeviceDescription}]");

                foreach (UsbHub hub in controller.Hubs)
                {
                    ShowHub(hub);
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Controller  = null");
            }
        }

        private static void ShowHub(UsbHub hub)
        {
            if (hub != null)
            {
                if (hub.IsRootHub)
                {
                    Console.WriteLine($"[{hub.DeviceDescription}]");
                }
                else
                {
                    Console.WriteLine($"Port[{hub.AdapterNumber}] DeviceConnected: {hub.DeviceDescription}");
                }

                foreach (Device device in hub.Devices)
                {
                    ShowDevice(device);
                }
            }
            else
            {
                Console.WriteLine("Hub = null");
            }
        }

        private static void ShowDevice(Device device)
        {
            if (device != null)
            {
                if (device is UsbHub)
                {
                    ShowHub((UsbHub)device);
                }
                else
                {
                    string s = "Port[" + device.AdapterNumber + "]";
                    if (device.IsConnected)
                    {
                        s += " DeviceConnected: " + device.DeviceDescription;
                    }
                    else
                    {
                        s += " NoDeviceConnected";
                    }

                    Console.WriteLine(s);
                }
            }
            else
            {
                Console.WriteLine("Device = null");
            }
        }
    }
}

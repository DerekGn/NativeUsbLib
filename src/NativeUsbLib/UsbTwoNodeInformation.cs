
namespace NativeUsbLib
{
    public class UsbTwoHubInformation : UsbHubInformation
    {
        private readonly UsbApi.UsbNodeInformation _usbNodeInformation;

        internal UsbTwoHubInformation(UsbApi.UsbNodeInformation usbNodeInformation)
        {
            _usbNodeInformation = usbNodeInformation;
        }

        public bool HubIsBusPowered => _usbNodeInformation.HubInformation.HubIsBusPowered;
    }
}

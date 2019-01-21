namespace UsbViewer.Extensions
{
    internal static class BoolExtensions
    {
        public static string Display(this bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}

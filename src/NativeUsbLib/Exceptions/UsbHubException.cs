using System;
using System.Collections.Generic;
using System.Text;

namespace NativeUsbLib.Exceptions
{
    [System.Serializable]
    public class UsbHubException : System.Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public UsbHubException()
        {
        }

        public UsbHubException(string message) : base(message)
        {
        }

        public UsbHubException(string message, System.Exception inner) : base(message, inner)
        {
        }

        protected UsbHubException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}

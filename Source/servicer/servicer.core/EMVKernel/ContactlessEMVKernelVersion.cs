using System.Collections.Generic;

namespace Servicer.Core.EMVKernel
{
    public class ContactlessEMVKernelVersion
    {
        public List<string> EntryPoint { get; set; }    // "EP"
        public string MasterCardPaypass { get; set; }   // "MK"
        public string VisaPayWave { get; set; }         // "VK"
        public string AmexExpressPay { get; set; }      // "AK"
        public string JCBJSpeedy { get; set; }          // "JK"
        public string DinersDPAS { get; set; }          // "DK"
        public string UnionPayQuickPass { get; set; }   // "CK"
        public string InteracFlash { get; set; }        // "IK"
        public string EPAL { get; set; }                // "EK"
        public string GemaltoPURE { get; set; }         // "GK"
        public string PagoBANCOMAT { get; set; }        // "PB"
    }
}

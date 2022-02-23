using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace XO.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TenderType
    {
        [Description("")]
        Invalid = 0,

        [Description("Visa")]
        Visa = 1,

        [Description("MasterCard")]
        MasterCard = 2,

        [Description("American Express")]
        AMEX = 3,

        [Description("Diners Club")]
        DinersClub = 4,

        [Description("enRoute")]
        enRoute = 5,

        [Description("Japan Credit Bureau")]
        JCB = 6,

        Discover = 7,
        ACH = 8,
        Debit = 9,

        [Description("PINless Debit")]
        DebitPINLess = 10,

        Cash = 17,
        Check = 18,

        [Description("FSA/HSA")]
        FSA_HSA = 20,

        [Description("Maestro")]
        Maestro = 21,

        [Description("Interac")]
        Interac = 22,

        [Description("DNA")]
        DNA = 23,
        // The rest of the codes are in TCLink. We are not using them at them moment.
        // THis is used when we do not have a Credit Card Number and prevents the string "INVALID" from showing up in response, etc.
        //[Description("")]
        //Refund         = 100,

        //Invalid_D      = 128,
        //Visa_D         = 129,

        //[Description("MasterCard-D")]
        //MasterCard_D   = 130,

        //[Description("American Express-D")]
        //AMEX_D         = 131,

        //[Description("Diners Club-D")]
        //DinersClub_D   = 132,

        //[Description("enRoute-D")]
        //enRoute_D      = 133,

        //[Description("JCB-D")]
        //JCB_D          = 134,

        //[Description("Discover-D")]
        //Discover_D     = 135,

        //[Description("ACH-D")]
        //ACH_D          = 136,

        //[Description("Debit Card")]
        //Debit_D        = 137,

        //[Description("PINless Debit-D")]
        //PINlessDebit_D = 138
    }

}

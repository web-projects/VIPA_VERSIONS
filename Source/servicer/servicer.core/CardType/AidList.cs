using Servicer.Core.Enums;
using XO.Enums;
using System.Collections.Generic;

namespace Servicer.Core.CardType
{
    public static class AidList
    {
        public const string CategoryDefault = "DEFAULT";
        public const string CategoryVital = "VITAL";
        public const string CategoryPaymentech = "PAYMENTECH-TANDEM";
        public const string CategoryFirstData = "FIRSTDATA-RAPIDCONNECT";
        public const string CategoryEvalon = "ELAVON";

        /// <summary>
        /// DEFAULT AIDS
        /// </summary>
        public static readonly List<AidEntry> DefaultAIDList = new List<AidEntry>()
        {
            new AidEntry("A0000001523010", TenderType.Discover, AIDType.None),          //Discover
            new AidEntry("A0000000042203", TenderType.MasterCard, AIDType.None),       //MasterCard U.S. Maestro common
            new AidEntry("A0000000980840", TenderType.Visa, AIDType.None),              //Visa common
            new AidEntry("A0000000043060", TenderType.MasterCard, AIDType.Debit),       //MasterCard International Maestro
            new AidEntry("A0000002771010", TenderType.Interac, AIDType.Debit),          //Interact
            new AidEntry("A0000006200620", TenderType.DNA, AIDType.Debit),              //DNA US Common Debit
            new AidEntry("A0000000041010", TenderType.MasterCard, AIDType.Credit),      //Mastercard Credit
        };

        /// <summary>
        /// VITAL AIDS
        /// </summary>
        public static readonly List<AidEntry> VitalAIDList = new List<AidEntry>()
        {
            new AidEntry("A0000000043060", TenderType.MasterCard, AIDType.Debit),       //MasterCard International Maestro
            new AidEntry("A0000000042203", TenderType.MasterCard, AIDType.Debit),       //MasterCard U.S. Maestro common
            new AidEntry("A0000000980840", TenderType.Visa, AIDType.Debit),             //Visa common
            new AidEntry("A0000000032010", TenderType.Visa, AIDType.None),              //Visa Electron
            new AidEntry("A0000000033010", TenderType.Visa, AIDType.Debit),             //Visa Interlink
            new AidEntry("A00000002501", TenderType.AMEX, AIDType.Credit),              //Amex
            new AidEntry("A0000001523010", TenderType.DinersClub, AIDType.Credit),      //Diners
            new AidEntry("A0000003241010", TenderType.Discover, AIDType.Credit),        //Discover
            new AidEntry("A0000000651010", TenderType.JCB, AIDType.Credit),             //JCB
            new AidEntry("A0000000041010", TenderType.MasterCard, AIDType.Credit),      //Mastercard Credit
            new AidEntry("A0000000031010", TenderType.Visa, AIDType.Credit),            //Visa Credit and Visa Debit International
        };

        /// <summary>
        /// PAYMENTECH-TANDEM AIDS
        /// </summary>
        public static readonly List<AidEntry> PaymanTechAIDList = new List<AidEntry>()
        {
            new AidEntry("A0000000043060", TenderType.MasterCard, AIDType.Debit),       //MasterCard International Maestro
            new AidEntry("A0000000042203", TenderType.MasterCard, AIDType.Debit),       //MasterCard U.S. Maestro common
            new AidEntry("A0000000980840", TenderType.Visa, AIDType.Debit),             //Visa common
            new AidEntry("A0000001523010", TenderType.Discover, AIDType.Debit),         //Discover
            new AidEntry("A0000000033010", TenderType.Visa, AIDType.Debit),             //Visa Interlink
            new AidEntry("A0000001524010", TenderType.Discover, AIDType.Debit),         //Discover Common Debit
            new AidEntry("A0000006200620", TenderType.DNA, AIDType.Debit),              //DNA US Common Debit
            new AidEntry("A00000002501", TenderType.AMEX, AIDType.Credit),              //Amex
            new AidEntry("A0000001523010", TenderType.DinersClub, AIDType.Credit),      //Diners
            new AidEntry("A0000003241010", TenderType.Discover, AIDType.Credit),        //Discover
            new AidEntry("A0000000651010", TenderType.JCB, AIDType.Credit),             //JCB
            new AidEntry("A0000000041010", TenderType.MasterCard, AIDType.Credit),      //Mastercard Credit
            new AidEntry("A0000000032010", TenderType.Visa, AIDType.Credit),            //Visa Electron
            new AidEntry("A0000000031010", TenderType.Visa, AIDType.Credit),            //Visa Credit and Visa Debit International
        };

        /// <summary>
        /// FIRSTDATA RAPID CONNECT AIDS
        /// </summary>
        public static readonly List<AidEntry> FirstDataRapidConnectAIDList = new List<AidEntry>()
        {
            new AidEntry("A0000000043060", TenderType.MasterCard, AIDType.Debit),      //MasterCard International Maestro
            new AidEntry("A0000000042203", TenderType.MasterCard, AIDType.None),       //MasterCard U.S. Maestro common
            new AidEntry("A0000000980840", TenderType.Visa, AIDType.Debit),            //Visa common
            new AidEntry("A0000001523010", TenderType.Discover, AIDType.None),         //Discover and DinersClub
            new AidEntry("A0000000033010", TenderType.Visa, AIDType.Debit),            //Visa Interlink
            new AidEntry("A0000001524010", TenderType.Discover, AIDType.Debit),        //Discover Common Debit
            new AidEntry("A0000006200620", TenderType.DNA, AIDType.Debit),             //DNA US Common Debit
            new AidEntry("A0000002771010", TenderType.Interac, AIDType.Debit),         //Interact

            new AidEntry("A00000002501", TenderType.AMEX, AIDType.Credit),             //Amex
            new AidEntry("A0000000041010", TenderType.MasterCard, AIDType.Credit),     //Mastercard Credit
            new AidEntry("A0000000032010", TenderType.Visa, AIDType.None),             //Visa Electron
            new AidEntry("A0000000031010", TenderType.Visa, AIDType.Credit),           //Visa Credit and Visa Debit International

            new AidEntry("A0000000651010", TenderType.JCB, AIDType.Credit),            //JCB
        };

        /// <summary>
        /// ELAVON AIDS
        /// </summary>
        public static readonly List<AidEntry> EvalonAIDList = new List<AidEntry>()
        {
            new AidEntry("A0000000043060", TenderType.MasterCard, AIDType.Debit),      //MasterCard International Maestro
            new AidEntry("A0000000042203", TenderType.MasterCard, AIDType.None),       //MasterCard U.S. Maestro common
            new AidEntry("A0000000980840", TenderType.Visa, AIDType.None),             //Visa common
            new AidEntry("A0000001523010", TenderType.Discover, AIDType.Debit),        //Discover
            new AidEntry("A0000000033010", TenderType.Visa, AIDType.Debit),            //Visa Interlink
            new AidEntry("A0000001524010", TenderType.Discover, AIDType.Debit),        //Discover Common Debit
            new AidEntry("A0000006200620", TenderType.DNA, AIDType.Debit),             //DNA US Common Debit
            new AidEntry("A0000002771010", TenderType.Interac, AIDType.Debit),         //Interact

            new AidEntry("A00000002501", TenderType.AMEX, AIDType.Credit),              //Amex
            new AidEntry("A0000001523010", TenderType.DinersClub, AIDType.Credit),      //Discover
            new AidEntry("A0000003241010", TenderType.Discover, AIDType.Credit),        //Discover
            new AidEntry("A0000000651010", TenderType.JCB, AIDType.Credit),             //JCB
            new AidEntry("A0000000041010", TenderType.MasterCard, AIDType.Credit),      //Mastercard Credit
            new AidEntry("A0000000032010", TenderType.Visa, AIDType.None),              //Visa Electron
            new AidEntry("A0000000031010", TenderType.Visa, AIDType.Credit),            //Visa Credit and Visa Debit International
        };

        // AIDS CATEGORIZED BY PAYMENT SERVICER
        public static Dictionary<string, List<AidEntry>> AIDList = new Dictionary<string, List<AidEntry>>()
        {
            [CategoryDefault] = (DefaultAIDList),                        // DEFAULT AIDS
            [CategoryVital] = (VitalAIDList),                            // VITAL AIDS
            [CategoryPaymentech] = (PaymanTechAIDList),                  // PAYMENTECH-TANDEM AIDS
            [CategoryFirstData] = (FirstDataRapidConnectAIDList),        // FIRSTDATA RAPID CONNECT AIDS
            [CategoryEvalon] = (EvalonAIDList),                          // ELAVON AIDS
        };
    }
}
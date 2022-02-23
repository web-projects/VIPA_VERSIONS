using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using XO.Enums;

namespace Servicer.Core.CardType
{
    public static class CardTypeMappingManager
    {
        //Regex format here for those interested
        //MasterCard, "^(2[3-6][0-9]{6,}|(222[1-9]|2720)[0-9]{4,}|(22[3-9]|27[01])[0-9]{5,}|5[1-5][0-9]{6,})$
        //Visa, "^4[0-9]{7,}$
        //AMEX, "^3[47][0-9]{5,}$
        //DinersClub, "^3(?:0[0-5]|[68][0-9])[0-9]{4,}$
        //JCB, "^(35[3-8][0-9]{5,}|352[89][0-9]{4,})$
        //Maestro, "^(50[0-9]{6,}|5[6-9][0-9]{6,}|6[0-9]{7,})$
        //Discover, I'm not doing regex for this ****

        internal static List<CardTypeMapData> CardTypeMapDataList { get; } = new List<CardTypeMapData>()
        {
            //TODO: load the card type mapping info from config or database
            new CardTypeMapData(222100, 272099, TenderType.MasterCard),
            ////The three commented out are deactivated, but might be reactivated in the soon™ in the future
            //new CardTypeMapData(300000, 305999, TenderType.DinersClub),
            //new CardTypeMapData(309500, 309599, TenderType.DinersClub),
            //new CardTypeMapData(380000, 389999, TenderType.DinersClub),
            new CardTypeMapData(360000, 369999, TenderType.DinersClub),
            new CardTypeMapData(370000, 379999, TenderType.AMEX),
            new CardTypeMapData(340000, 349999, TenderType.AMEX),
            new CardTypeMapData(352800, 358999, TenderType.JCB),
            //The three deactived diner's club range is taken by discover (currently)
            new CardTypeMapData(300000, 305999, TenderType.Discover),
            new CardTypeMapData(308800, 310299, TenderType.Discover),
            new CardTypeMapData(311200, 312099, TenderType.Discover),
            new CardTypeMapData(315800, 315999, TenderType.Discover),
            new CardTypeMapData(333700, 334999, TenderType.Discover),
            new CardTypeMapData(352800, 358999, TenderType.Discover),
            new CardTypeMapData(360000, 369999, TenderType.Discover),
            new CardTypeMapData(380000, 399999, TenderType.Discover),
            new CardTypeMapData(400000, 499999, TenderType.Visa),
            new CardTypeMapData(500000, 509999, TenderType.Maestro),
            new CardTypeMapData(510000, 559999, TenderType.MasterCard),
            new CardTypeMapData(601100, 601109, TenderType.Discover),
            new CardTypeMapData(601120, 601149, TenderType.Discover),
            new CardTypeMapData(601174, 601174, TenderType.Discover),
            new CardTypeMapData(601177, 601179, TenderType.Discover),
            new CardTypeMapData(601186, 601199, TenderType.Discover),
            new CardTypeMapData(620000, 629999, TenderType.Discover),
            new CardTypeMapData(644000, 650599, TenderType.Discover),
            new CardTypeMapData(650600, 650600, TenderType.Discover),
            new CardTypeMapData(650610, 659999, TenderType.Discover),
            new CardTypeMapData(810000, 817199, TenderType.Discover),
            new CardTypeMapData(560000, 699999, TenderType.Maestro),
            ///Why is Maestro place last (and out of order)? 
            ///Because Maestro has a lot of overlap with discover, which is coincidentally excluded in list below
            ///by placing this last, we ensure discovery is found first and don't return maestro wrongly
        };

        internal static readonly List<Tuple<int, int>> MaestroExclusion = new List<Tuple<int, int>>()
        {
            new Tuple<int, int>(601100, 601109),
            new Tuple<int, int>(601120, 601149),
            new Tuple<int, int>(601174, 601174),
            new Tuple<int, int>(601177, 601179),
            new Tuple<int, int>(601186, 601199),
            new Tuple<int, int>(622126, 622925),
            new Tuple<int, int>(624000, 626999),
            new Tuple<int, int>(628200, 628899),
            new Tuple<int, int>(644000, 649999),
            new Tuple<int, int>(650000, 659999),
        };

        public static TenderType GetTenderType(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
                return TenderType.Invalid;

            string paddedCardNum = cardNumber.PadRight(6, '0').Left(6); //cardNumber always comes in 6 digits, but just in case
            int.TryParse(paddedCardNum, out int cardDigits);
            TenderType result = CardTypeMapDataList
                .FirstOrDefault(x => x.Start <= cardDigits && x.End >= cardDigits)?.Tender ?? TenderType.Invalid;

            if (result == TenderType.Maestro && MaestroExclusion.Any(x => x.Item1 <= cardDigits && x.Item2 >= cardDigits))
            {
                //It's very unlikely since most would fall under discover, but there are some ranges can still happen 
                result = TenderType.Invalid;
            }

            return result;
        }

        public static TenderType GetTenderTypeFromMediaType(string tcMediaType)
        {
            if (string.IsNullOrWhiteSpace(tcMediaType))
            {
                return TenderType.Invalid;
            }

            switch (tcMediaType.ToLower())
            {
                case "amex":
                case "american express":
                return TenderType.AMEX;
                case "visa":
                return TenderType.Visa;
                case "diner":
                case "diners club":
                case "dinersclub":
                return TenderType.DinersClub;
                case "mc":
                case "mastercard":
                case "maestro":
                return TenderType.MasterCard;
                case "discover":
                case "japan credit bureau":
                case "jcb":
                case "disc":
                return TenderType.Discover;
                default:
                return TenderType.Invalid;
            }
        }
    }
}

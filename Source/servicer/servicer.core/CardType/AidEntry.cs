using Servicer.Core.Enums;
using XO.Enums;

namespace Servicer.Core.CardType
{
    public sealed class AidEntry
    {
        public string AIDValue { get; }
        public TenderType CardBrand { get; }
        public AIDType AidType { get; }

        public AidEntry(string aIDValue, TenderType cardBrand, AIDType aidType = AIDType.None)
        {
            AIDValue = aIDValue;
            CardBrand = cardBrand;
            AidType = aidType;
        }
    }
}

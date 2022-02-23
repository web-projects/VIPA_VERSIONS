using XO.Enums;

namespace Servicer.Core.CardType
{
    internal class CardTypeMapData
    {
        public int Start { get; }
        public int End { get; }
        public TenderType Tender { get; }

        public CardTypeMapData(int start, int end, TenderType tender)
        {
            Tender = tender;
            Start = start;
            End = end;
        }
    }
}

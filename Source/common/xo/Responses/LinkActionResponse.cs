using Common.XO.Private;
using System.Collections.Generic;

namespace Common.XO.Responses
{
    public class LinkActionResponse
    {
        //Matches LinkActionRequest MessageID value
        public string MessageID { get; set; }
        public string RequestID { get; set; }

        public List<LinkErrorValue> Errors { get; set; }

        //PaymentResponse only used when request Action = 'Payment'; can be null
        public LinkPaymentResponse PaymentResponse { get; set; }

        //PaymentUpdateResponse only used when request Action = 'PaymentUpdate'; can be null
        //public LinkPaymentUpdateResponse PaymentUpdateResponse { get; set; }

        //DALResponse only used when request Action = 'DALStatus' or Action = 'Payment' and a DAL workflow was involved in processing the request; can be null
        public LinkDALActionResponse DALActionResponse { get; set; }
        public LinkDALResponse DALResponse { get; set; }

        //public LinkSessionResponse SessionResponse { get; set; }
        public LinkEventResponse EventResponse { get; set; }
    }
}

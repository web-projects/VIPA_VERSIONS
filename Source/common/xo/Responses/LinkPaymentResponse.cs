using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.XO.Responses
{
    public class LinkPaymentResponse
    {
        public List<LinkErrorValue> Errors { get; set; }
        public LinkPaymentResponseStatus? Status { get; set; }
        public string EntryModeStatus { get; set; }
        //public List<LinkNameValueResponse> TCLinkResponse { get; set; }
        public string TCTransactionID { get; set; }
        public DateTime? TCTimestamp { get; set; }
        public long? CollectedAmount { get; set; }
        public string BillingID { get; set; }
        //public LinkCardResponse CardResponse { get; set; }
        //public LinkEMVResponse EMVResponse { get; set; }
        //public LinkBankAccountResponse AccountResponse { get; set; }
        //public LinkReceiptResponse ReceiptResponse { get; set; }
        //public LinkIIASResponse IIASResponse { get; set; }
        public string CancelType { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkPaymentResponseStatus
    {
        Error,
        Approved,
        Accepted,
        Declined,
        Cancelled,
        InProgress,
        NotFound,
        PaymentTimeout
    }
}

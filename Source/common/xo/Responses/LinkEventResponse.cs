using Common.Helpers;
using Newtonsoft.Json.Converters;
using System;
using System.Text.Json.Serialization;

namespace Common.XO.Responses
{
    public class LinkEventResponse
    {
        public string EventType { get; set; }
        public string EventCode { get; set; }
        public string Description { get; set; }
        public Guid EventID { get; set; }
        public int OrdinalID { get; set; }
        public string[] EventData { get; set; }

        //Event Type
        [JsonConverter(typeof(StringEnumConverter))]
        public enum EventTypeType
        {
            [StringValue("Display")]
            DISPLAY,
            [StringValue("Input")]
            INPUT,
            [StringValue("Payment")]
            PAYMENT,
            [StringValue("PaymentUpdate")]
            PAYMENTUPDATE,
            [StringValue("Device")]
            DEVICE,
            [StringValue("ManualEntry")]
            MANUALENTRY
        }

        //Event Code
        [JsonConverter(typeof(StringEnumConverter))]
        public enum EventCodeType
        {
            DEVICE_MESSAGE_DISPLAY,
            DEVICE_MESSAGE_PRESENT_CARD,
            DEVICE_MESSAGE_INSERT_CARD,
            DEVICE_MESSAGE_INSERT_CARD_AGAIN,
            DEVICE_MESSAGE_SWIPE_CARD,
            DEVICE_MESSAGE_MANUAL_ENTRY,
            DEVICE_MESSAGE_CHOOSE_CARD_TYPE,
            DEVICE_MESSAGE_ENTER_ZIP,
            DEVICE_MESSAGE_REMOVE_CARD,
            DEVICE_MESSAGE_ENTER_PIN,
            DEVICE_DEBIT_SELECTED,
            DEVICE_CREDIT_SELECTED,
            DEVICE_CARD_CANNOT_READ,
            DEVICE_CARD_INSERTED,
            DEVICE_CARD_REMOVED,
            DEVICE_CARD_PRESENTED_CONTACTLESS,
            DEVICE_CARD_PRESENTED_CARD_IDENTIFIER,
            DEVICE_CARD_PRESENTED_CARD_IDENTIFIER_GUID,
            DEVICE_CARD_PRESENTED_WHITELIST_PAN,
            DEVICE_CARD_SWIPED,
            DEVICE_KEY_PRESSED,
            DEVICE_ZIP_ENTERED,
            DEVICE_PIN_ENTERED,
            DEVICE_REQUEST_CARDTYPE,
            DEVICE_REQUEST_CARD_INSERT,
            DEVICE_REQUEST_CARD_REMOVE,
            DEVICE_REQUEST_KEY_PRESS,
            DEVICE_REQUEST_ZIP_ENTER,
            DEVICE_REQUEST_PIN_ENTER,
            PAYMENT_COMPLETE,
            PAYMENT_OFFLINE,
            PAYMENT_ONLINE,
            PAYMENT_PROCESSING,
            PAYMENT_STATUS,
            USER_CANCELED,
            REQUEST_TIMEOUT,
            CANCELLATION_REQUEST,
            DEVICE_UNPLUGGED,
            DEVICE_NOT_FOUND,
            DEVICE_VERIFY_AMOUNT_APPROVED,
            DEVICE_VERIFY_AMOUNT_DECLINED,
            DEVICE_MESSAGE_PRESENT_CHECK,
            DEVICE_MESSAGE_CHECK_CANNOT_READ,
            DEVICE_MESSAGE_CHECK_READ,
            DEVICE_MESSAGE_UNSUPPORTED_CARD,
            DEVICE_MESSAGE_BAD_CARD,
            DEVICE_MESSAGE_PIN_BYPASSED,
            DEVICE_MESSAGE_PIN_ENTRY_CANCELED,
            DEVICE_PLUGGED
        }
    }
}

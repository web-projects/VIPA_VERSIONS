using Common.Helpers;

namespace Servicer.Core.Enums
{
    public enum ServicerDataProviderType
    {
        Local,
        Cloud
    }

    public enum SessionDataType
    {
        IPASession
    }
    public enum PaymentDataType
    {
        IPAPayment
    }

    public enum LinkRequestProviderType
    {
        IPALinkRequest
    }

    public enum DALDataType
    {
        IPADALStatus
    }

    public enum MicroServiceTypeAndState
    {
        DALEndADA,
        DALGetStatus,
        DALNeedCardData,
        DALCouldNotReadCardData,
        DALNeedCreditDebitResponse,
        DALNeedZIP,
        DALNeedPIN,
        DALNeedRemoveCard,
        DALStartADA,
        DALDeviceUI,
        DALUpdatePaymentCancel,
        DALManualPayment,
        MonitorCollectCardholderInfo,
        ProcessorNeedAuthentication,
        ProcessorNeedResponse,
        ReceiverActionComplete,
        NotCalculated,
        NowhereActionComplete,
        ErrorUnableToDetermine,
        DALStartPreSwipe,
        DALWaitForCardPreSwipe,
        DALEndPreSwipe,
        DALPurgeHeldCardData,
        MonitorUpdateMessage,
        DALNeedVerifyAmount,
        MonitorWaitACHData,
        DALUpdateACHCancel,
        DALNeedSignature,
        MonitorCancelRequest,
        DalNeedCardInsertTapData,
        DALSetDeviceToIdle,
        ProcessorSendSignature,
    }

    public enum AuthenticationStatus
    {
        Unknown,
        Failure,
        Success,
        BearerToken
    }

    public enum CreditDebitType
    {
        Credit,
        Debit,
        Unknown
    }

    public enum DALStatus
    {
        Unknown,
        Unchecked,
        Ready,
        NotFound,
        FoundNotReady
    }

    public enum AVSRule
    {
        None,
        Amount,
        Zip
    }

    public enum CancelType
    {
        None,
        Cancel,
        Void,
        Credit,
        Reversal,
        ChargeBack,
        Autovoid
    }

    public enum AuthorizationMode
    {
        ISSUER,
        CARD
    }

    public enum ServicerType
    {
        Local,
        Cloud
    }
    public enum LinkPaymentResponseCode
    {
        None,
        InvalidPinOrPinLengthError = 117,
        InvalidPinOrPinLengthErrorCtls = 500,       //CORE is returning different error code for ctls PIN
    }
    public enum AIDType
    {
        [StringValue(null)]       //not specified, can be both, use null for default to fall through
        None = 0x0,

        [StringValue("credit")]
        Credit = 0x1,

        [StringValue("debit")]
        Debit = 0x2,
    };
}

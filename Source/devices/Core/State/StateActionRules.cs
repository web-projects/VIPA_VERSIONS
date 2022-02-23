namespace Devices.Core.State
{
    internal class StateActionRules
    {
        public bool IsBusy { get; set; }
        public bool NeedsDeviceRecovery { get; set; }
        public bool UserCanceledTransaction { get; set; }
        public bool TransactionCancellationRequest { get; set; }
    }
}

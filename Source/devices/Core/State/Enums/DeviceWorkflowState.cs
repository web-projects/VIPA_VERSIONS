namespace Devices.Core.State.Enums
{
    public enum DeviceWorkflowState
    {
        /// <summary>
        /// Initial state that kicks off the entire workflow for DAL.
        /// </summary>
        None,

        /// <summary>
        /// Represents a state when the workflow is attempting to recover a device
        /// whether it be from a failed initialization state or disconnect of the device.
        /// </summary>
        DeviceRecovery,

        /// <summary>
        /// Represents a state when the workflow is obtains the health status of the target device.
        /// </summary>
        InitializeDeviceHealth,

        /// <summary>
        /// Represents a state where the device is detected and successfully connected to.
        /// </summary>
        InitializeDeviceCommunication,

        /// <summary>
        /// Represents a state when the device is currently idle and awaiting some commands in order
        /// to process incoming requests or handle device events that need to be posted.
        /// </summary>
        Manage,

        /// <summary>
        /// Represents a state where a request is processed into a sub-workflow.
        /// </summary>
        ProcessRequest,

        /// <summary>
        /// Represents a state where the main controller has given over responsibility
        /// to a sub-controller in order to properly handle the request that has been 
        /// received.
        /// </summary>
        SubWorkflowIdleState,

        /// <summary>
        /// Represents a state when DAL is currently being shutdown and cleanup must take place including
        /// disconnection from server if applicable.
        /// </summary>
        Shutdown
    }
}

namespace Devices.Sdk.Features.Internal.State
{ 
    public enum DALWorkflowState
    {
        /// <summary>
        /// Initial state that kicks off the entire workflow for DAL.
        /// </summary>
        None,

        /// <summary>
        /// Represents a state when extended DAL features are discovered and loaded up.
        /// </summary>
        FeatureDiscovery,

        /// <summary>
        /// Represents a state when the workflow is attempting to recover a device
        /// whether it be from a failed initialization state or disconnect of the device.
        /// </summary>
        DeviceRecovery,

        /// <summary>
        /// Represents a state where the device is detected and successfully connected to.
        /// </summary>
        InitializeDeviceCommunication,

        /// <summary>
        /// Represents a state when the device was detected and successfully connected to
        /// but the device health is currently being checked.
        /// </summary>
        InitializeDeviceHealth,

        /// <summary>
        /// Represents a state during startup when application data must be saved to
        /// database
        /// </summary>
        SaveRollCall,

        /// <summary>
        /// Represents a state during startup when device information must be saved to
        /// database
        /// </summary>
        RegisterDevice,

        /// <summary>
        /// Represents a state where connection to the remote server was lost or never established
        /// and we want to wait for a specified amount of time before we try to advance forward again.
        /// </summary>
        AttemptServerReconnect,

        /// <summary>
        /// Represents a state when the device has been successfully initialized and validated
        /// and now we are attempting to connect to the server (Listener).
        /// </summary>
        ConnectToServer,

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

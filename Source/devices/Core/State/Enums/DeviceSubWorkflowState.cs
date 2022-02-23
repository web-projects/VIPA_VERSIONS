using System;

namespace Devices.Core.State.Enums
{
    /// <summary>
    /// Represents a set of sub-workflow states that represent certain specific
    /// processes that need to be completed before a transition occurs to send us
    /// back to the Manage state (Idle).
    /// </summary>
    public enum DeviceSubWorkflowState
    {
        /// <summary>
        /// Default state for all SubWorkflows.
        /// </summary>
        Undefined,

        /// <summary>
        /// Represents a state when DAL requests a custom screen be displayed on Device
        /// </summary>
        DisplayIdleScreen,

        /// <summary>
        /// Represents a state when DAL queries the device for VIPA bundle versions
        /// </summary>
        ReportVIPAVersions,

        /// <summary>
        /// Represents a state where a sanity check is performed to ensure that the DAL
        /// is in an operational state ready to receive the next command before a response
        /// is sent back to the caller.
        /// </summary>
        SanityCheck,

        /// <summary>
        /// Represents a state when SubWorkflow Completes
        /// </summary>
        RequestComplete
    }
}

using System;

namespace Devices.Sdk.Features.State
{
    /// <summary>
    /// Represents a set of sub-workflow states that represent certain specific
    /// processes that need to be completed before a transition occurs to send us
    /// back to the Manage state (Idle).
    /// </summary>
    public enum DALSubWorkflowState : int
    {
        /// <summary>
        /// Default state for all SubWorkflows.
        /// </summary>
        Undefined,

        /// <summary>
        /// Represents a state when DAL asks the device to display VIPA versions
        /// </summary>
        ReportVipaVersions,

        /// <summary>
        /// Represents a state when DAL asks the device for returning to idle state
        /// </summary>
        SetDeviceIdle,

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

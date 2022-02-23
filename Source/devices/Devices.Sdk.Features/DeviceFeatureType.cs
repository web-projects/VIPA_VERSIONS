namespace Devices.Sdk.Features
{
    public enum DeviceFeatureType : byte
    {
        /// <summary>
        /// Defines an undefined DAL feature type.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Defines a workflow feature that contains a set of state actions.
        /// </summary>
        Workflow,

        /// <summary>
        /// Defines an interrupt feature that forces a slight break in process
        /// from the currently executing device request. Such interrupts can be
        /// for retrieving specific information from a device in flight.
        /// </summary>
        Interrupt
    }
}

using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;
using System;
using System.Collections.Generic;

namespace Devices.Sdk.Features
{
    internal interface IDeviceWorkflowFeatureBagOfStateContainer : IDisposable
    {
        int NumberOfAvailableStates { get; }
        void Accept(IDictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> bagOfStates);
    }
}

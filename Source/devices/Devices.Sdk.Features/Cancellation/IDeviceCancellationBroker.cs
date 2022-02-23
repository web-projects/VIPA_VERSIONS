using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Sdk.Features.Cancellation
{
    public interface IDeviceCancellationBroker
    {
        /// <summary>
        /// Set the success outcome callback that should be executed on successful invocations.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        IDeviceCancellationBroker SetSuccessOutcomeCallback(Action action);

        /// <summary>
        /// Set the failure outcome callback that should be executed on failure invocations.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        IDeviceCancellationBroker SetFailureOutcomeCallback(Action action);

        /// <summary>
        /// Executes a specific action asynchronously with the acceptance of a cancellation token.
        /// </summary>
        /// <returns></returns>
        Task<PolicyResult<TOutput>> ExecuteWithCancellationAsync<TOutput>(Func<CancellationToken, TOutput> func, CancellationToken relatedToken);

        /// <summary>
        /// Executes a specific action asynchronously with the acceptance of a cancellation token.
        /// </summary>
        /// <returns></returns>
        Task<PolicyResult<TOutput>> ExecuteWithTimeoutAsync<TOutput>(Func<CancellationToken, TOutput> func, int timeout, CancellationToken relatedToken);
    }
}

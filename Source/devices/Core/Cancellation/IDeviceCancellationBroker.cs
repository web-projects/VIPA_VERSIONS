using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Core.Cancellation
{
    internal interface IDeviceCancellationBroker
    {
        /// <summary>
        /// Executes a specific action asynchronously with the acceptance of a cancellation token.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="relatedToken">
        /// The related token is another token that will be leveraged aside from Polly's own token.
        /// In most cases this is likely the cancellation token that you requested from your controller.
        /// </param>
        /// <returns></returns>
        Task<PolicyResult<TOutput>> ExecuteWithTimeoutAsync<TOutput>(Func<CancellationToken, TOutput> func, int timeout, CancellationToken relatedToken);
    }
}

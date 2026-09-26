// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Adds typed failed-partition recovery without changing the existing <see cref="IReactors"/> contract.
/// </summary>
public static class ReactorPartitionRecoveryExtensions
{
    /// <summary>
    /// Retry a failed partition for a registered reactor.
    /// </summary>
    /// <typeparam name="TReactor">The reactor type.</typeparam>
    /// <param name="reactors">The reactor registry.</param>
    /// <param name="partition">The failed partition.</param>
    /// <returns>The bounded outcome of the retry request.</returns>
    /// <exception cref="ReactorPartitionRecoveryNotSupported">The reactor registry does not support partition recovery.</exception>
    public static Task<ReactorPartitionRetryOutcome> RetryFailedPartitionFor<TReactor>(this IReactors reactors, Partition partition)
        where TReactor : IReactor
    {
        if (reactors is not IReactorPartitionRecovery recovery)
        {
            throw new ReactorPartitionRecoveryNotSupported();
        }

        return recovery.RetryFailedPartitionFor(typeof(TReactor), partition);
    }
}

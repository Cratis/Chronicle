// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Shares projection build and subscription admission across managers on one host.
/// </summary>
public interface IProjectionSubscriptionThrottle
{
    /// <summary>
    /// Waits for admission within the caller's remaining budget.
    /// </summary>
    /// <param name="timeout">The remaining queue budget, or <see cref="Timeout.InfiniteTimeSpan"/> for background repair.</param>
    /// <param name="cancellationToken">The activation's cancellation token.</param>
    /// <returns>Whether admission was granted.</returns>
    /// <exception cref="OperationCanceledException">The activation cancelled its wait.</exception>
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Releases exactly one granted admission.
    /// </summary>
    void Release();
}

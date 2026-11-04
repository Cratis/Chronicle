// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Admits at most eight projection build and subscription operations per host.
/// </summary>
/// <remarks>
/// No wait handle is used. Managed lifetime deliberately avoids disposal racing another activation's release.
/// </remarks>
[Singleton]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The host gate never uses AvailableWaitHandle; retaining the managed semaphore avoids disposal racing activation releases.")]
public class ProjectionSubscriptionThrottle : IProjectionSubscriptionThrottle
{
    readonly SemaphoreSlim _admission = new(8, 8);

    /// <inheritdoc/>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _admission.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc/>
    public void Release() => _admission.Release();
}

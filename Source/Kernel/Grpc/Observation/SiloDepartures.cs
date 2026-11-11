// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="ISiloDepartures"/> that follows the cluster membership of this silo.
/// </summary>
/// <remarks>
/// Only a silo that becomes <see cref="SiloStatus.Dead"/> counts as departed. One that is shutting down still hosts the
/// activations it has not deactivated yet, so checking subscriptions then would find them intact and miss the loss.
/// </remarks>
[Singleton]
public sealed class SiloDepartures : ISiloDepartures, IDisposable
{
    readonly CancellationTokenSource _stopped = new();
    readonly Lock _lock = new();
    TaskCompletionSource _next = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Initializes a new instance of the <see cref="SiloDepartures"/> class.
    /// </summary>
    /// <param name="membership">The <see cref="IClusterMembershipService"/> to follow.</param>
    /// <param name="logger">The <see cref="ILogger"/> for logging.</param>
    public SiloDepartures(IClusterMembershipService membership, ILogger<SiloDepartures> logger)
    {
        _ = Follow(membership, logger, _stopped.Token);
    }

    /// <inheritdoc/>
    public Task Next
    {
        get
        {
            lock (_lock)
            {
                return _next.Task;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stopped.Cancel();
        _stopped.Dispose();
    }

    /// <summary>
    /// Gets the silos that are not dead in a snapshot.
    /// </summary>
    /// <param name="snapshot">The <see cref="ClusterMembershipSnapshot"/>.</param>
    /// <returns>The silos that are not dead.</returns>
    internal static HashSet<SiloAddress> LiveSilos(ClusterMembershipSnapshot snapshot) =>
        snapshot.Members.Values.Where(member => member.Status != SiloStatus.Dead).Select(member => member.SiloAddress).ToHashSet();

    async Task Follow(IClusterMembershipService membership, ILogger logger, CancellationToken stopped)
    {
        HashSet<SiloAddress>? live = null;
        try
        {
            await foreach (var snapshot in membership.MembershipUpdates.WithCancellation(stopped))
            {
                var nowLive = LiveSilos(snapshot);
                var departed = live?.Where(silo => !nowLive.Contains(silo)).ToArray() ?? [];
                live = nowLive;
                if (departed.Length > 0)
                {
                    logger.SilosLeftCheckingClientSubscriptions(departed.Select(silo => silo.ToString()));
                    Signal();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The silo is stopping.
        }
        catch (Exception ex)
        {
            // Subscriptions are still checked at the safety-net interval; only the early check after a departure is lost.
            logger.FollowingClusterMembershipFailed(ex);
        }
    }

    void Signal()
    {
        TaskCompletionSource departed;
        lock (_lock)
        {
            departed = _next;
            _next = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        departed.TrySetResult();
    }
}

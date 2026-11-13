// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="ISiloDepartures"/> that follows the cluster membership of this silo.
/// </summary>
/// <remarks>
/// <para>
/// A silo counts as departing twice: when it starts shutting down (<see cref="SiloStatus.ShuttingDown"/> or
/// <see cref="SiloStatus.Stopping"/>) and when it is declared <see cref="SiloStatus.Dead"/> or disappears from the
/// membership. A gracefully stopping silo deactivates its activations while it shuts down, and every call that reaches
/// one of its observers then activates it again on a surviving silo without its subscription. Signalling at the start
/// lets every keeper check through the whole shutdown, and the second signal at death extends that window to cover
/// what the surviving silos deactivate once they learn of it. A silo that crashes is only signalled at death.
/// </para>
/// <para>
/// The settle window rests on how this kernel runs Orleans today: the classic grain directory, where the surviving
/// silos deactivate the activations whose directory entries a dead silo owned shortly after they learn of its death and
/// a joining silo takes entries over without deactivating anything; no activation repartitioner or rebalancer; and no
/// memory-based activation shedding. Enabling any of these makes joins or rebalancing lose subscriptions with nothing
/// but the safety-net interval to restore them.
/// </para>
/// <para>
/// When following the membership fails, it is followed again after a backoff. The membership seen last is kept, so a
/// silo that left in between is still signalled.
/// </para>
/// </remarks>
public sealed class SiloDepartures : ISiloDepartures, IDisposable
{
    /// <summary>
    /// The wait before following the membership again after the first failure; it doubles up to <see cref="LastRetry"/>.
    /// </summary>
    internal static readonly TimeSpan FirstRetry = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The longest wait before following the membership again.
    /// </summary>
    internal static readonly TimeSpan LastRetry = TimeSpan.FromSeconds(30);

    readonly CancellationTokenSource _stopped = new();
    readonly Lock _lock = new();
    readonly ILogger<SiloDepartures> _logger;
    TaskCompletionSource _next = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Dictionary<SiloAddress, Stage>? _known;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiloDepartures"/> class.
    /// </summary>
    /// <param name="membership">The <see cref="IClusterMembershipService"/> to follow.</param>
    /// <param name="logger">The <see cref="ILogger"/> for logging.</param>
    public SiloDepartures(IClusterMembershipService membership, ILogger<SiloDepartures> logger)
        : this(membership, logger, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SiloDepartures"/> class.
    /// </summary>
    /// <param name="membership">The <see cref="IClusterMembershipService"/> to follow.</param>
    /// <param name="logger">The <see cref="ILogger"/> for logging.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to wait with before following the membership again.</param>
    internal SiloDepartures(IClusterMembershipService membership, ILogger<SiloDepartures> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _ = Follow(membership, timeProvider, _stopped.Token);
    }

    enum Stage
    {
        Member = 0,
        Leaving = 1,
        Gone = 2
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
    /// Takes in a membership snapshot and signals when a silo seen before started leaving or left.
    /// </summary>
    /// <param name="snapshot">The <see cref="ClusterMembershipSnapshot"/>.</param>
    /// <remarks>
    /// Silos are only compared with what was seen before, so the first snapshot, and a silo that first appears already
    /// shutting down or dead, signal nothing. Called by one membership follower at a time.
    /// </remarks>
    internal void Observe(ClusterMembershipSnapshot snapshot)
    {
        var stages = snapshot.Members.Values.ToDictionary(member => member.SiloAddress, member => StageOf(member.Status));
        var departing = _known?
            .Where(known => (stages.TryGetValue(known.Key, out var stage) ? stage : Stage.Gone) > known.Value)
            .Select(known => known.Key)
            .ToArray() ?? [];
        _known = stages;

        if (departing.Length > 0)
        {
            _logger.SilosLeavingCheckingClientSubscriptions(departing.Select(silo => silo.ToString()));
            Signal();
        }
    }

    static Stage StageOf(SiloStatus status) => status switch
    {
        SiloStatus.ShuttingDown or SiloStatus.Stopping => Stage.Leaving,
        SiloStatus.Dead => Stage.Gone,
        _ => Stage.Member
    };

    async Task Follow(IClusterMembershipService membership, TimeProvider timeProvider, CancellationToken stopped)
    {
        var retry = FirstRetry;
        while (!stopped.IsCancellationRequested)
        {
            try
            {
                await foreach (var snapshot in membership.MembershipUpdates.WithCancellation(stopped))
                {
                    Observe(snapshot);
                    retry = FirstRetry;
                }

                return;
            }
            catch (OperationCanceledException) when (stopped.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Until it is followed again, subscriptions are only checked at the safety-net interval.
                _logger.FollowingClusterMembershipFailed(retry, ex);
            }

            try
            {
                await Task.Delay(retry, timeProvider, stopped);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retry = retry * 2 > LastRetry ? LastRetry : retry * 2;
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionReservation"/>.
/// </summary>
/// <remarks>
/// The state is in memory only. The grain is kept active while a connection is held and for
/// <see cref="ConnectionOwnership.ReleasedClaimRetention"/> after the last one is released, following the same rules
/// as <see cref="ConnectionOwnership"/>.
/// </remarks>
/// <param name="clusterMembership"><see cref="IClusterMembershipService"/> for telling which silos are still active.</param>
public class ConnectionReservation(IClusterMembershipService clusterMembership) : Grain, IConnectionReservation
{
    static readonly TimeSpan _pruneInterval = TimeSpan.FromMinutes(1);

    readonly Dictionary<SiloAddress, int> _streams = [];
    ConnectionOwner? _owner;
    DateTimeOffset? _releasedAt;

    /// <summary>
    /// Gets or sets the clock used for the retention of released claims.
    /// </summary>
    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(PruneInactiveSilos, new() { DueTime = _pruneInterval, Period = _pruneInterval });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo)
    {
        if (owner.IsUnidentified) return Task.FromResult(false);

        var now = Clock.GetUtcNow();
        DropInactiveSilos(now, silo);

        if (_owner is { } current && !current.Equals(owner))
        {
            if (_streams.Count > 0 || _releasedAt is null || now - _releasedAt < ConnectionOwnership.ReleasedClaimRetention)
            {
                return Task.FromResult(false);
            }

            _streams.Clear();
        }

        _owner = owner;
        _releasedAt = null;
        _streams[silo] = _streams.GetValueOrDefault(silo) + 1;
        Keep(TimeSpan.MaxValue);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task Release(ConnectionOwner owner, SiloAddress silo)
    {
        if (_owner is { } current && current.Equals(owner) && _streams.TryGetValue(silo, out var streams))
        {
            if (streams <= 1) _streams.Remove(silo);
            else _streams[silo] = streams - 1;

            if (_streams.Count == 0) MarkReleased(Clock.GetUtcNow());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Keeps the grain active for a period.
    /// </summary>
    /// <param name="period">How long to keep it active. <see cref="TimeSpan.MaxValue"/> keeps it active until changed.</param>
    internal virtual void Keep(TimeSpan period) => DelayDeactivation(period);

    Task PruneInactiveSilos(CancellationToken cancellationToken)
    {
        DropInactiveSilos(Clock.GetUtcNow(), null);
        return Task.CompletedTask;
    }

    void DropInactiveSilos(DateTimeOffset now, SiloAddress? except)
    {
        if (_streams.Count == 0) return;

        var members = clusterMembership.CurrentSnapshot.Members;
        var inactive = _streams.Keys
            .Where(silo => !silo.Equals(except) && !(members.TryGetValue(silo, out var member) && member.Status == SiloStatus.Active))
            .ToArray();

        foreach (var silo in inactive) _streams.Remove(silo);
        if (inactive.Length > 0 && _streams.Count == 0) MarkReleased(now);
    }

    void MarkReleased(DateTimeOffset now)
    {
        _releasedAt = now;
        Keep(ConnectionOwnership.ReleasedClaimRetention);
    }
}

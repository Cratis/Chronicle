// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionReservation"/>.
/// </summary>
/// <remarks>
/// The state is in memory only. Every claim is a lease that lapses unless its holder renews it, so a claim that is
/// never released - a lost call, a holder that died - ends by itself, and a grain that was reactivated empty is filled
/// again by the next renewal. The grain is kept active while a connection is held and for
/// <see cref="ConnectionOwnership.ReleasedClaimRetention"/> after the last one ends, following the same rules as
/// <see cref="ConnectionOwnership"/>.
/// </remarks>
/// <param name="clusterMembership"><see cref="IClusterMembershipService"/> for telling which silos are still active.</param>
public class ConnectionReservation(IClusterMembershipService clusterMembership) : Grain, IConnectionReservation
{
    static readonly TimeSpan _pruneInterval = TimeSpan.FromMinutes(1);

    readonly Dictionary<Guid, Lease> _leases = [];
    ConnectionOwner? _owner;
    DateTimeOffset? _releasedAt;

    /// <summary>
    /// Gets or sets the clock used for leases and the retention of released claims.
    /// </summary>
    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(Prune, new() { DueTime = _pruneInterval, Period = _pruneInterval });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
    {
        if (owner.IsUnidentified || lease <= TimeSpan.Zero) return Task.FromResult(false);

        var now = Clock.GetUtcNow();
        DropEndedLeases(now, silo);

        if (_owner is { } current && !current.Equals(owner))
        {
            if (_leases.Count > 0 || _releasedAt is null || now - _releasedAt < ConnectionOwnership.ReleasedClaimRetention)
            {
                return Task.FromResult(false);
            }

            _leases.Clear();
        }

        _owner = owner;
        _releasedAt = null;
        _leases[token] = new(silo, now + lease);
        Keep(TimeSpan.MaxValue);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task Release(ConnectionOwner owner, Guid token)
    {
        if (_owner is { } current && current.Equals(owner) && _leases.Remove(token) && _leases.Count == 0)
        {
            MarkReleased(Clock.GetUtcNow());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Keeps the grain active for a period.
    /// </summary>
    /// <param name="period">How long to keep it active. <see cref="TimeSpan.MaxValue"/> keeps it active until changed.</param>
    internal virtual void Keep(TimeSpan period) => DelayDeactivation(period);

    Task Prune(CancellationToken cancellationToken)
    {
        DropEndedLeases(Clock.GetUtcNow(), null);
        return Task.CompletedTask;
    }

    void DropEndedLeases(DateTimeOffset now, SiloAddress? except)
    {
        if (_leases.Count == 0) return;

        var members = clusterMembership.CurrentSnapshot.Members;
        var ended = _leases
            .Where(_ => _.Value.ExpiresAt <= now || (!_.Value.Silo.Equals(except) && !(members.TryGetValue(_.Value.Silo, out var member) && member.Status == SiloStatus.Active)))
            .ToArray();
        if (ended.Length == 0) return;

        // A lease that lapsed counts as released when it lapsed. One whose silo is gone counts from when that was noticed.
        var releasedAt = ended.Max(_ => _.Value.ExpiresAt <= now ? _.Value.ExpiresAt : now);
        foreach (var lease in ended) _leases.Remove(lease.Key);
        if (_leases.Count == 0) MarkReleased(releasedAt);
    }

    void MarkReleased(DateTimeOffset releasedAt)
    {
        _releasedAt = releasedAt;
        Keep(ConnectionOwnership.ReleasedClaimRetention);
    }

    sealed record Lease(SiloAddress Silo, DateTimeOffset ExpiresAt);
}

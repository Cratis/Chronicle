// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Observation.given;

public class a_connection_held_on_another_silo : Specification
{
    protected static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    protected readonly Guid _token = Guid.NewGuid();
    protected SiloAddress _siloA;
    protected SiloAddress _siloB;
    protected HashSet<SiloAddress> _active;
    protected ManualClock _clock;
    protected Reservation _held;

    async Task Establish()
    {
        _siloA = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _siloB = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _active = [_siloA, _siloB];
        _clock = new();
        var membership = Substitute.For<IClusterMembershipService>();
        membership.CurrentSnapshot.Returns(_ => new ClusterMembershipSnapshot(
            _active.ToImmutableDictionary(silo => silo, silo => new ClusterMember(silo, SiloStatus.Active, "silo")), new MembershipVersion(1)));
        _held = new(membership) { Clock = _clock };
        await _held.Claim(AuthenticatedCalls.OwnerOf("alice"), _siloA, _token, Lease);
    }

    protected sealed class Reservation(IClusterMembershipService membership) : ConnectionReservation(membership)
    {
        internal override void Keep(TimeSpan period)
        {
        }

        internal override void DeactivateWhenIdle()
        {
        }
    }

    protected sealed class ManualClock : TimeProvider
    {
        DateTimeOffset _now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}

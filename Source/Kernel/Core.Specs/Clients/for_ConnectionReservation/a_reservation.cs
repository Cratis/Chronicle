// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class a_reservation : Specification
{
    protected static readonly ConnectionOwner Alice = new("issuer", "alice");
    protected static readonly ConnectionOwner Bob = new("issuer", "bob");
    protected static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    protected SiloAddress _first;
    protected SiloAddress _second;
    protected ManualClock _clock;
    protected IClusterMembershipService _membership;
    protected TestReservation _reservation;
    protected Guid _token;
    protected Guid _otherToken;
    protected HashSet<SiloAddress> _active;

    void Establish()
    {
        _first = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _second = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 2);
        _active = [_first, _second];
        _clock = new();
        _membership = Substitute.For<IClusterMembershipService>();
        _membership.CurrentSnapshot.Returns(_ => new ClusterMembershipSnapshot(
            _active.ToImmutableDictionary(silo => silo, silo => new ClusterMember(silo, SiloStatus.Active, silo.ToString())),
            new MembershipVersion(1)));
        _token = Guid.NewGuid();
        _otherToken = Guid.NewGuid();
        _reservation = NewGrain();
    }

    protected TestReservation NewGrain() => new(_membership) { Clock = _clock };

    protected sealed class TestReservation(IClusterMembershipService membership) : ConnectionReservation(membership)
    {
        internal override void Keep(TimeSpan period)
        {
        }
    }

    protected sealed class ManualClock : TimeProvider
    {
        DateTimeOffset _now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}

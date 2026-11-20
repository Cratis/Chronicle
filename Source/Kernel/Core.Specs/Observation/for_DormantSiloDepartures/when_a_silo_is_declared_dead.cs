// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_DormantSiloDepartures;

public class when_a_silo_is_declared_dead : Specification
{
    static readonly SiloAddress _stays = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
    static readonly SiloAddress _leaves = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);

    IDormantTargetSweep _sweep;
    IClusterMembershipService _membership;
    ClusterMembershipSnapshot _snapshot;
    DormantSiloDepartures _departures;
    int _first;
    int _second;
    int _third;

    static ClusterMembershipSnapshot Snapshot(long version, params (SiloAddress Silo, SiloStatus Status)[] members) =>
        new(members.ToImmutableDictionary(member => member.Silo, member => new ClusterMember(member.Silo, member.Status, member.Silo.ToString())), new MembershipVersion(version));

    async Task Establish()
    {
        _sweep = Substitute.For<IDormantTargetSweep>();
        _sweep.SilosLeft(Arg.Any<IReadOnlyCollection<string>>()).Returns(2);
        _membership = Substitute.For<IClusterMembershipService>();
        _snapshot = Snapshot(1, (_stays, SiloStatus.Active), (_leaves, SiloStatus.Active));
        _membership.CurrentSnapshot.Returns(_ => _snapshot);
        _departures = new(_sweep, _membership, Substitute.For<ILogger<DormantSiloDepartures>>());
        await _departures.CheckOnce();
    }

    async Task Because()
    {
        _first = await _departures.CheckOnce();
        _snapshot = Snapshot(2, (_stays, SiloStatus.Active), (_leaves, SiloStatus.Dead));
        _second = await _departures.CheckOnce();
        _third = await _departures.CheckOnce();
    }

    [Fact] void should_sweep_nothing_while_the_membership_is_unchanged() => _first.ShouldEqual(0);
    [Fact] void should_sweep_for_the_silo_that_left() => _sweep.Received(1).SilosLeft(Arg.Is<IReadOnlyCollection<string>>(silos => silos.Count == 1 && silos.Contains(_leaves.ToParsableString())));
    [Fact] void should_report_how_many_observers_it_woke() => _second.ShouldEqual(2);
    [Fact] void should_not_sweep_for_the_same_silo_twice() => _third.ShouldEqual(0);
}

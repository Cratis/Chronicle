// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_DormantSiloDepartures;

public class when_this_silo_is_the_one_leaving : Specification
{
    static readonly SiloAddress _thisSilo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
    static readonly SiloAddress _other = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);

    IDormantTargetSweep _sweep;
    ClusterMembershipSnapshot _snapshot;
    DormantSiloDepartures _departures;
    int _woken;

    static ClusterMembershipSnapshot Snapshot(long version, SiloStatus thisStatus) =>
        new(
            ImmutableDictionary<SiloAddress, ClusterMember>.Empty
                .Add(_thisSilo, new ClusterMember(_thisSilo, thisStatus, "this"))
                .Add(_other, new ClusterMember(_other, SiloStatus.Active, "other")),
            new MembershipVersion(version));

    async Task Establish()
    {
        _sweep = Substitute.For<IDormantTargetSweep>();
        var membership = Substitute.For<IClusterMembershipService>();
        _snapshot = Snapshot(1, SiloStatus.Active);
        membership.CurrentSnapshot.Returns(_ => _snapshot);
        var local = Substitute.For<ILocalSiloDetails>();
        local.SiloAddress.Returns(_thisSilo);
        _departures = new(_sweep, membership, local, Substitute.For<ILogger<DormantSiloDepartures>>());
        await _departures.CheckOnce();
    }

    async Task Because()
    {
        _snapshot = Snapshot(2, SiloStatus.Dead);
        _woken = await _departures.CheckOnce();
    }

    [Fact] void should_sweep_nothing_because_what_it_would_wake_goes_down_with_it() => _woken.ShouldEqual(0);
    [Fact] void should_not_ask_the_sweep() => _sweep.DidNotReceiveWithAnyArgs().SilosLeft(default!, default);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures.given;

/// <summary>
/// Two active silos, with membership snapshots handed straight to <see cref="SiloDepartures.Observe"/> so each one is
/// processed before the specification looks at the outcome.
/// </summary>
public class two_silos : Specification
{
    protected SiloAddress _first;
    protected SiloAddress _second;
    protected SiloDepartures _departures;
    int _version;

    void Establish()
    {
        _first = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _second = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.MembershipUpdates.Returns(Channel.CreateUnbounded<ClusterMembershipSnapshot>().Reader.ReadAllAsync());
        _departures = new(membership, NullLogger<SiloDepartures>.Instance, new Observation.given.ManualClock());
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Active));
    }

    void Destroy() => _departures.Dispose();

    protected void Publish(params (SiloAddress Silo, SiloStatus Status)[] members) =>
        _departures.Observe(new ClusterMembershipSnapshot(
            members.ToImmutableDictionary(member => member.Silo, member => new ClusterMember(member.Silo, member.Status, member.Silo.ToString())),
            new MembershipVersion(++_version)));
}

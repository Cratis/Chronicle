// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures.given;

public class two_silos : Specification
{
    protected static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);
    protected SiloAddress _first;
    protected SiloAddress _second;
    protected Channel<ClusterMembershipSnapshot> _updates;
    protected SiloDepartures _departures;
    int _version;

    void Establish()
    {
        _first = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _second = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
        _updates = Channel.CreateUnbounded<ClusterMembershipSnapshot>();
        var membership = Substitute.For<IClusterMembershipService>();
        membership.MembershipUpdates.Returns(_updates.Reader.ReadAllAsync());
        _departures = new(membership, NullLogger<SiloDepartures>.Instance);
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Active));
    }

    void Destroy() => _departures.Dispose();

    protected void Publish(params (SiloAddress Silo, SiloStatus Status)[] members) =>
        _updates.Writer.TryWrite(new ClusterMembershipSnapshot(
            members.ToImmutableDictionary(member => member.Silo, member => new ClusterMember(member.Silo, member.Status, member.Silo.ToString())),
            new MembershipVersion(++_version)));

    protected static async Task<bool> CompletesWithin(Task task, TimeSpan within)
    {
        try
        {
            await task.WaitAsync(within);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}

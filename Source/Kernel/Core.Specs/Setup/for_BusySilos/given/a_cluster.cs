// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;

namespace Cratis.Chronicle.Setup.for_BusySilos.given;

public class a_cluster : Specification
{
    protected SiloAddress _thisSilo;
    protected SiloAddress _activeSibling;
    protected SiloAddress _deadSibling;
    protected BusySilos _busySilos;
    protected BusySilo? _result;

    void Establish()
    {
        _thisSilo = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.1"), 11111), 42);
        _activeSibling = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.2"), 11111), 7);
        _deadSibling = SiloAddress.New(new IPEndPoint(IPAddress.Parse("10.0.0.3"), 11111), 3);

        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(_thisSilo);
        var membership = Substitute.For<IClusterMembershipService>();
        membership.CurrentSnapshot.Returns(new ClusterMembershipSnapshot(
            new Dictionary<SiloAddress, ClusterMember>
            {
                [_thisSilo] = new(_thisSilo, SiloStatus.Active, "this"),
                [_activeSibling] = new(_activeSibling, SiloStatus.Active, "active"),
                [_deadSibling] = new(_deadSibling, SiloStatus.Dead, "dead")
            }.ToImmutableDictionary(),
            new MembershipVersion(1)));

        _busySilos = new(localSilo, membership);
    }

    /// <summary>
    /// Creates the timeout Orleans reports for a request that got no answer, in the shape it writes it.
    /// </summary>
    /// <param name="target">The target silo as written in the request.</param>
    /// <returns>The <see cref="TimeoutException"/>.</returns>
    protected TimeoutException TimeoutTo(string target) =>
        new($"Response did not arrive on time in 00:00:30 for message: Request [{_thisSilo} sys.svc.user/startup]->[{target} eventstoresubscriptionsmanager/Studio] Cratis.Chronicle.Observation.EventStoreSubscriptions.IEventStoreSubscriptionsManager.Ensure() #1234. ");
}

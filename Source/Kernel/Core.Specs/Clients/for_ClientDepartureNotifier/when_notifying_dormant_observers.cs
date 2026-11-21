// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients.for_ClientDepartureNotifier;

public class when_notifying_dormant_observers : Specification
{
    IDormantTargetSweep _sweep;
    ClientDepartureNotifier _notifier;

    void Establish()
    {
        _sweep = Substitute.For<IDormantTargetSweep>();
        _notifier = new(Substitute.For<IGrainFactory>(), Substitute.For<ILogger<ClientDepartureNotifier>>(), _sweep);
    }

    async Task Because() => await _notifier.NotifyDormant(new ConnectionId("the-connection"), []);

    [Fact] void should_hand_the_connection_to_the_sweep_of_dormant_targets() => _sweep.Received(1).ConnectionLeft(new ConnectionId("the-connection"), Arg.Any<IReadOnlyCollection<Concepts.Observation.ObserverKey>>());
}

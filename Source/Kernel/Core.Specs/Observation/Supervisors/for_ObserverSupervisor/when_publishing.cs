// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Supervisors.for_ObserverSupervisor;

public class when_publishing : given.a_supervisor
{
    readonly ObserverRevisionAcknowledgements _acknowledgements = new(2, []);
    ObserverRevisionAcknowledgements _result;

    void Establish()
    {
        _stateProvider.GetStoredRevision(_eventStore, _observerId).Returns(new ObserverDefinitionRevision(4));
        _routingClient.Publish(_eventStore, _observerId, new ObserverDefinitionRevision(4)).Returns(_acknowledgements);
    }

    async Task Because() => _result = await _supervisor.Publish();

    [Fact] void should_publish_the_stored_revision_and_answer_with_the_acknowledgements() => _result.ShouldEqual(_acknowledgements);
}

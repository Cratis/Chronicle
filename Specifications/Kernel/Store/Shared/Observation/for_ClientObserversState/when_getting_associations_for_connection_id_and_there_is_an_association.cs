// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ClientObserversState;

public class when_getting_associations_for_connection_id_and_there_is_an_association : given.client_observers_state
{
    IEnumerable<ObserverFullyQualifiedIdentifier> observers;
    ObserverFullyQualifiedIdentifier expected;

    void Establish()
    {
        expected = new(observer_id, new(microservice_id, tenant_id, event_sequence_id));
        state.AssociateObserverWithConnectionId(connection_id, expected);
    }

    void Because() => observers = state.GetObserversForConnectionId(connection_id);

    [Fact] void should_have_one_item() => observers.Count().ShouldEqual(1);
    [Fact] void should_hold_expected_observer() => observers.First().ShouldEqual(expected);
}

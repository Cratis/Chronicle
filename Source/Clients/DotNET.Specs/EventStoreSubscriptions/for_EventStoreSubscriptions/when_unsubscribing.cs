// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Microsoft.Extensions.Logging;
using IContractEventStoreSubscriptions = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions;

public class when_unsubscribing : Specification
{
    IChronicleConnection _connection;
    IEventStore _eventStore;
    IEventTypes _eventTypes;
    Chronicle.EventStoreSubscriptions.EventStoreSubscriptions _subscriptions;
    IChronicleServicesAccessor _servicesAccessor;
    EventStoreSubscriptionId _subscriptionId;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = _connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(Substitute.For<IServices>());
        _servicesAccessor.Services.EventStoreSubscriptions.Returns(Substitute.For<IContractEventStoreSubscriptions>());
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(new EventStoreName("target-event-store"));
        _eventStore.Connection.Returns(_connection);
        _subscriptionId = "my-subscription";
        _subscriptions = new Chronicle.EventStoreSubscriptions.EventStoreSubscriptions(
            _eventTypes,
            _eventStore,
            Substitute.For<ILogger<Chronicle.EventStoreSubscriptions.EventStoreSubscriptions>>());
    }

    async Task Because() => await _subscriptions.Unsubscribe(_subscriptionId);

    [Fact] void should_call_remove_on_services_with_correct_target_event_store() =>
        _servicesAccessor.Services.EventStoreSubscriptions.Received(1).Remove(Arg.Is<RemoveEventStoreSubscriptions>(r =>
            r.TargetEventStore == _eventStore.Name));

    [Fact] void should_call_remove_on_services_with_correct_subscription_id() =>
        _servicesAccessor.Services.EventStoreSubscriptions.Received(1).Remove(Arg.Is<RemoveEventStoreSubscriptions>(r =>
            r.SubscriptionIds.Single() == _subscriptionId.Value));
}

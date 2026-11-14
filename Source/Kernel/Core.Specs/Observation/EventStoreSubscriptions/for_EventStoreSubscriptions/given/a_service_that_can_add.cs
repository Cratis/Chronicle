// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using ContractEventStoreSubscriptionDefinition = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class a_service_that_can_add : an_event_store_subscriptions_service
{
    protected IEventSequence _eventSequence;
    protected IEventStoreSubscriptionsManager _subscriptionsManager;
    protected IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> _existing = [];

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _eventSequence.Append(
            Arg.Any<Concepts.Events.EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Concepts.Events.Tag>>(),
            Arg.Any<Concepts.Events.EventSourceType>(),
            Arg.Any<Concepts.Events.EventStreamType>(),
            Arg.Any<Concepts.Events.EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), Concepts.Events.EventSequenceNumber.First));

        _subscriptionsManager = Substitute.For<IEventStoreSubscriptionsManager>();
        _subscriptionsManager.GetSubscriptionDefinitions().Returns(_ => _existing);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(Arg.Any<string>()).Returns(_subscriptionsManager);
    }

    protected static AddEventStoreSubscriptions RequestWith(params string[] sourceNamespaces) => new()
    {
        TargetEventStore = "target-event-store",
        Subscriptions =
        [
            new ContractEventStoreSubscriptionDefinition
            {
                Identifier = "test-subscription-id",
                SourceEventStore = "source-event-store",
                EventTypes = [],
                SourceNamespaces = [.. sourceNamespaces]
            }
        ]
    };

    protected Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition ExistingWith(params string[] sourceNamespaces) =>
        new(new EventStoreSubscriptionId("test-subscription-id"), new Concepts.EventStoreName("source-event-store"), [])
        {
            SourceNamespaces = sourceNamespaces.Select(_ => new Concepts.EventStoreNamespaceName(_)).ToArray()
        };
}

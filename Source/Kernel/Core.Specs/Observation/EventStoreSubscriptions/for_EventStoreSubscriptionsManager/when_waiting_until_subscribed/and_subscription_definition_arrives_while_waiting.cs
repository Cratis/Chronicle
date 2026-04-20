// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

/// <summary>
/// Proves the fix for the race condition where the gRPC <c>Add</c> handler calls
/// <c>WaitUntilSubscribed</c> before the <c>EventStoreSubscriptionsReactor</c> has processed
/// the <c>EventStoreSubscriptionAdded</c> event and written the definition into grain state.
/// The old code threw <see cref="InvalidOperationException"/> immediately; the fix polls until
/// the definition appears.
/// </summary>
public class and_subscription_definition_arrives_while_waiting : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    EventStoreSubscriptionDefinition _definition;
    Exception _error;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        var observer = Substitute.For<IObserver>();
        observer.IsSubscribed().Returns(true);
        _silo.AddProbe(_ => observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        _definition = new EventStoreSubscriptionDefinition(
            new EventStoreSubscriptionId(SourceEventStore),
            new EventStoreName(SourceEventStore),
            [new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First)]);
    }

    async Task Because()
    {
        // Start waiting BEFORE the definition exists in state — this is the race condition:
        // the gRPC Add handler calls WaitUntilSubscribed immediately after appending
        // EventStoreSubscriptionAdded, but the reactor that calls manager.Add() is asynchronous.
        var waitTask = _manager.WaitUntilSubscribed(
            new EventStoreSubscriptionId(SourceEventStore),
            TimeSpan.FromSeconds(2));

        // Let the polling loop spin a few iterations without finding the definition.
        await Task.Delay(50);

        // Simulate the reactor delivering the definition (as EventStoreSubscriptionsReactor.Added does).
        await _manager.Add(_definition);

        // WaitUntilSubscribed should now find the definition and return successfully.
        _error = await Catch.Exception(() => waitTask);
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}

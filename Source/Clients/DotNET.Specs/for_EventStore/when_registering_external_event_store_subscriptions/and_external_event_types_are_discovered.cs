// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_external_event_types_are_discovered : Specification
{
    const string SourceEventStore = "StudioAdmin";
    static readonly MethodInfo _registerExternalSubscriptionsMethod =
        typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    EventStore _eventStore;
    IEventStoreSubscriptions _subscriptions;

    void Establish()
    {
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();

        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([]);

        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Reactors.Returns([typeof(external_event_reactor)]);
        clientArtifactsProvider.Reducers.Returns([]);

        var eventTypes = new EventTypesForSpecifications([typeof(external_user_invited)]);

        _eventStore = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        SetField("_eventStoreName", new EventStoreName("Lobby"));
        SetField("_clientArtifactsProvider", clientArtifactsProvider);
        SetAutoProperty("EventTypes", eventTypes);
        SetAutoProperty("Projections", projections);
        SetAutoProperty("Subscriptions", _subscriptions);
    }

    async Task Because() => await (Task)_registerExternalSubscriptionsMethod.Invoke(_eventStore, [])!;

    [Fact] void should_use_source_event_store_name_as_subscription_id() =>
        _subscriptions.Received(1).Subscribe(
            Arg.Is<EventStoreSubscriptionId>(id => id.Value == SourceEventStore),
            SourceEventStore,
            Arg.Any<Action<IEventStoreSubscriptionBuilder>>());

    [Fact] void should_not_use_auto_prefixed_subscription_id() =>
        _subscriptions.DidNotReceive().Subscribe(
            Arg.Is<EventStoreSubscriptionId>(id => id.Value.StartsWith("auto-", StringComparison.Ordinal)),
            Arg.Any<string>(),
            Arg.Any<Action<IEventStoreSubscriptionBuilder>>());

    void SetField(string fieldName, object value)
    {
        var field = typeof(EventStore).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(_eventStore, value);
    }

    void SetAutoProperty(string propertyName, object value)
    {
        var field = typeof(EventStore).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(_eventStore, value);
    }

    [EventType("f6d8a40f-db16-4474-9f1e-d7ce6f263a2f")]
    [EventStore(SourceEventStore)]
    record external_user_invited;

    [Reactor]
    class external_event_reactor : IReactor
    {
        Task On(external_user_invited @event) => Task.CompletedTask;
    }
}

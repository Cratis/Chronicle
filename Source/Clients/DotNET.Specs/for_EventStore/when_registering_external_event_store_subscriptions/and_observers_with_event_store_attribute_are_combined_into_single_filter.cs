// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_observers_with_event_store_attribute_are_combined_into_single_filter : Specification
{
    const string SourceEventStore = "StudioAdmin";

    static readonly MethodInfo _registerExternalSubscriptionsMethod =
        typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    EventStore _eventStore;
    IEventStoreSubscriptions _subscriptions;
    Action<IEventStoreSubscriptionBuilder>? _configure;
    EventTypeId _invitedEventTypeId;
    EventTypeId _acceptedEventTypeId;

    void Establish()
    {
        _subscriptions = Substitute.For<IEventStoreSubscriptions>();
        _subscriptions
            .Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => _configure = callInfo.Arg<Action<IEventStoreSubscriptionBuilder>>());

        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([]);

        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Reactors.Returns([typeof(external_user_invited_reactor), typeof(external_user_accepted_reactor)]);
        clientArtifactsProvider.Reducers.Returns([typeof(external_user_status_reducer)]);

        var eventTypes = new EventTypesForSpecifications([typeof(external_user_invited), typeof(external_user_accepted)]);
        _invitedEventTypeId = eventTypes.GetEventTypeFor(typeof(external_user_invited)).Id;
        _acceptedEventTypeId = eventTypes.GetEventTypeFor(typeof(external_user_accepted)).Id;

        _eventStore = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        SetField("_eventStoreName", new EventStoreName("Lobby"));
        SetField("_clientArtifactsProvider", clientArtifactsProvider);
        SetAutoProperty("EventTypes", eventTypes);
        SetAutoProperty("Projections", projections);
        SetAutoProperty("Subscriptions", _subscriptions);
    }

    async Task Because() => await (Task)_registerExternalSubscriptionsMethod.Invoke(_eventStore, [])!;

    [Fact] void should_register_a_single_subscription_for_the_observer_event_store() =>
        _subscriptions.Received(1).Subscribe(
            Arg.Is<EventStoreSubscriptionId>(id => id.Value == SourceEventStore),
            SourceEventStore,
            Arg.Any<Action<IEventStoreSubscriptionBuilder>>());

    [Fact] void should_combine_all_observer_event_types_into_the_subscription_filter()
    {
        var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
        builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder);

        _configure.ShouldNotBeNull();
        _configure!(builder);

        builder.Received(1).WithEventType(_invitedEventTypeId);
        builder.Received(1).WithEventType(_acceptedEventTypeId);
    }

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
    record external_user_invited;

    [EventType("7bc5b4c8-f957-44e7-b0bc-cf80ef6dbf7c")]
    record external_user_accepted;

    [EventStore(SourceEventStore)]
    [Reactor]
    class external_user_invited_reactor : IReactor
    {
        Task On(external_user_invited @event) => Task.CompletedTask;
    }

    [EventStore(SourceEventStore)]
    [Reactor]
    class external_user_accepted_reactor : IReactor
    {
        Task On(external_user_accepted @event) => Task.CompletedTask;
    }

    [EventStore(SourceEventStore)]
    [Reducer]
    class external_user_status_reducer : IReducerFor<object>
    {
        public object Reduce(external_user_invited @event, object? current) => new();
    }
}

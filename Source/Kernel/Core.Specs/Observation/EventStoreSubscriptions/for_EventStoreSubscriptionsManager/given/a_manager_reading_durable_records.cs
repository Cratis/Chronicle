// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.given;

/// <summary>
/// A manager for target event store "Target" with namespaces Default, Tenant1 and Tenant2, subscribed to event store "Source",
/// that reads the durable subscription records of its observers. Every observer counts the calls it receives.
/// </summary>
public class a_manager_reading_durable_records : Specification
{
    protected const string SubscriptionName = "subscription";
    protected static readonly string[] Namespaces = ["Default", "Tenant1", "Tenant2"];
    protected static readonly EventType EventType = new("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First);

    protected EventStoreSubscriptionsManager _manager;
    protected TestKitSilo _silo;
    protected IObserverDefinitionsStorage _definitions;
    protected IDurableSubscriptionRecords _records;
    protected readonly Dictionary<string, IObserver> _observers = [];
    protected readonly HashSet<string> _recorded = [];
    protected bool _recordsCanBeRelied = true;
    protected bool _observersAreSubscribed;

    protected int ObserverCalls => _observers.Values.Sum(observer => observer.ReceivedCalls().Count());

    protected static EventStoreSubscriptionDefinition Definition(params EventType[] eventTypes) =>
        new(SubscriptionName, "Source", eventTypes.Length == 0 ? [EventType] : eventTypes);

    protected static ObserverKey KeyFor(string @namespace) => new($"{SubscriptionName}->Target", "Source", @namespace, EventSequenceId.Outbox);

    protected async Task CreateManager()
    {
        _silo = new TestKitSilo();
        _silo.AddNamespaceCasing();
        _definitions = _silo.AddObserverDefinitions();
        _definitions.Get(Arg.Any<ObserverId>()).Returns(ObserverDefinition.Empty with { EventTypes = [EventType] });
        _silo.AddService(Substitute.For<ILocalSiloDetails>());

        _records = Substitute.For<IDurableSubscriptionRecords>();
        _records.CanBeRelied.Returns(_ => _recordsCanBeRelied);
        _records.Get(Arg.Any<ObserverKey>()).Returns(call =>
            Task.FromResult<ObserverDurableSubscription?>(_recorded.Contains(call.Arg<ObserverKey>().Namespace.Value)
                ? new ObserverDurableSubscription(ObserverSubscriptionKind.EventStoreSubscription, false, null, [])
                : null));
        _silo.AddService(_records);

        var target = Substitute.For<INamespaces>();
        target.GetAll().Returns(Namespaces.Select(name => new EventStoreNamespaceName(name)).ToArray());
        _silo.AddProbe<INamespaces>(_ => target);
        _silo.AddProbe<IObserver>(identity =>
        {
            var key = identity.ToString();
            if (!_observers.TryGetValue(key, out var observer))
            {
                observer = Substitute.For<IObserver>();
                observer.IsSubscribed().Returns(_ => _observersAreSubscribed);
                _observers[key] = observer;
            }

            return observer;
        });
        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>("Target");
    }

    protected IObserver ObserverOf(string @namespace) => _observers[KeyFor(@namespace).ToString()];

    protected bool WasSubscribed(string @namespace) =>
        ObserverOf(@namespace).ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe));

    protected Task Tick() => _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);
}

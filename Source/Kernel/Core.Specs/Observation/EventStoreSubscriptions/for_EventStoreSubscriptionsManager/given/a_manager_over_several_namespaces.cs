// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.BroadcastChannel;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.given;

/// <summary>
/// A manager for target event store "Target" holding namespaces Default, Tenant1, Tenant2 and the malformed "raalabs,raalabs",
/// subscribed to event store "Source" whose existing namespaces are Default, Tenant1 and Tenant2.
/// </summary>
public class a_manager_over_several_namespaces : Specification
{
    protected const string SubscriptionName = "subscription";
    protected const string Malformed = "raalabs,raalabs";

    protected EventStoreSubscriptionsManager _manager;
    protected TestKitSilo _silo;
    protected readonly List<string> _requestedKeys = [];
    protected readonly Dictionary<string, IObserver> _observers = [];
    protected readonly List<string> _sourceNamespacesRequested = [];
    protected bool _observersAreSubscribed;
    protected bool _sourceHasNamespaces = true;
    protected Func<NamespaceAdded, Task> _onNamespaceAdded;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddNamespaceCasing();
        _silo.AddService(Substitute.For<ILocalSiloDetails>());

        var target = Substitute.For<INamespaces>();
        target.GetAll().Returns([EventStoreNamespaceName.Default, "Tenant1", "Tenant2", Malformed]);
        var source = Substitute.For<INamespaces>();
        source.GetCanonical(Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            var name = call.Arg<EventStoreNamespaceName>();
            _sourceNamespacesRequested.Add(name.Value);
            return Task.FromResult<EventStoreNamespaceName?>(_sourceHasNamespaces ? name : null);
        });
        _silo.AddProbe<INamespaces>(identity => identity.ToString().Contains("Target") ? target : source);
        _silo.AddProbe<IObserver>(identity =>
        {
            var key = identity.ToString();
            _requestedKeys.Add(key);
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

    protected static string KeyFor(string @namespace) => new ObserverKey($"{SubscriptionName}->Target", "Source", @namespace, EventSequenceId.Outbox).ToString();

    protected static EventStoreSubscriptionDefinition DefinitionFor(params string[] sourceNamespaces) =>
        new(SubscriptionName, "Source", [new EventType("customer", 1)])
        {
            SourceNamespaces = sourceNamespaces.Select(_ => new EventStoreNamespaceName(_)).ToArray()
        };

    protected IEnumerable<string> RequestedKeys => _requestedKeys.Distinct();

    protected bool WasSubscribed(string @namespace) =>
        _observers.TryGetValue(KeyFor(@namespace), out var observer)
        && observer.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe));

    protected async Task AttachNamespaceAdded()
    {
        var channel = Substitute.For<IBroadcastChannelSubscription>();
        channel.ChannelId.Returns(ChannelId.Create("namespaces", "Target"));
        channel.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>())).Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        await _manager.OnSubscribed(channel);
    }
}

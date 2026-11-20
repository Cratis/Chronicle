// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep.given;

/// <summary>
/// A sweep over one event store and namespace holding two dormant observers: one served from a connection and silo that are
/// about to leave, one served from others.
/// </summary>
public class a_dormant_target_sweep : Specification
{
    protected const string EventStore = "store";
    protected const string Namespace = "namespace";
    protected const string LeavingConnection = "leaving-connection";
    protected const string LeavingSilo = "10.0.0.1:11111@1";

    protected static readonly ObserverKey _held = new("held", EventStore, Namespace, EventSequenceId.Log);
    protected static readonly ObserverKey _other = new("other", EventStore, Namespace, EventSequenceId.Log);

    protected DormantTargetSweep _sweep;
    protected IObserver _heldObserver;
    protected IObserver _otherObserver;
    protected IDurableSubscriptionRecords _records;
    protected IWakeAdmission _wakeAdmission;

    protected virtual bool RecordsCanBeRelied => true;

    protected virtual bool SiloIsStopping => false;

    void Establish()
    {
        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns([new NamespaceState(Namespace, DateTimeOffset.UnixEpoch)]);
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        definitions.GetAll().Returns([
            new ObserverDefinition("held", [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true),
            new ObserverDefinition("other", [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)
        ]);
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Namespaces.Returns(namespaces);
        eventStore.Observers.Returns(definitions);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([(EventStoreName)EventStore]);
        storage.GetEventStore(EventStore).Returns(eventStore);

        _records = Substitute.For<IDurableSubscriptionRecords>();
        _records.CanBeRelied.Returns(RecordsCanBeRelied);
        IReadOnlyDictionary<ObserverId, ObserverDurableSubscription> stored = new Dictionary<ObserverId, ObserverDurableSubscription>
        {
            ["held"] = new(ObserverSubscriptionKind.ClientReactor, false, null, [new(LeavingConnection, LeavingSilo)]),
            ["other"] = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("another-connection", "10.0.0.2:11111@1")])
        };
        _records.GetAll(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<bool>()).Returns(Task.FromResult(stored));

        _heldObserver = Substitute.For<IObserver>();
        _otherObserver = Substitute.For<IObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IObserver>(_held.ToString(), Arg.Any<string>()).Returns(_heldObserver);
        grainFactory.GetGrain<IObserver>(_other.ToString(), Arg.Any<string>()).Returns(_otherObserver);

        _wakeAdmission = new WakeAdmission(0, Environment.ProcessorCount);
        _sweep = new DormantTargetSweep(storage, grainFactory, _records, _wakeAdmission, Substitute.For<ILogger<DormantTargetSweep>>(), StopBudget())
        {
            CollectFor = TimeSpan.FromMilliseconds(1)
        };
    }

    IShutdownDrainBudget StopBudget()
    {
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.IsStopping.Returns(SiloIsStopping);
        return budget;
    }
}

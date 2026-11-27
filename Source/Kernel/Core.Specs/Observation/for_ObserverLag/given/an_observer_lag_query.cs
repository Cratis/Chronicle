// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.given;

public class an_observer_lag_query : Specification
{
    protected const string Store = "store";
    protected const string Namespace = "tenant";
    protected static readonly ObserverId Id = "observer";

    protected static readonly ObserverDefinition Definition = new(Id, [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);

    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IObserver _grain;
    protected IManagementGrain _directory;
    protected IDormantObservers _dormant;
    protected ObserverKey _key;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grain = Substitute.For<IObserver>();
        _directory = Substitute.For<IManagementGrain>();
        _dormant = Substitute.For<IDormantObservers>();
        _key = new ObserverKey(Id, Store, Namespace, EventSequenceId.Log);
        _grainFactory.GetGrain<IObserver>(_key).Returns(_grain);
        _grainFactory.GetGrain<IManagementGrain>(0).Returns(_directory);
        DirectoryReturns(SiloAddress.Zero);
        _storage.GetEventStore(Store).Observers.Has(Id).Returns(true);
        _storage.GetEventStore(Store).Observers.Get(Id).Returns(Definition);
        _storage.GetEventStore(Store).GetNamespace(Namespace).Observers.Get(Id).Returns(new ObserverState { RunningState = ObserverRunningState.Active });
    }

    protected void DirectoryReturns(SiloAddress? address)
    {
#pragma warning disable CA2012 // NSubstitute consumes the configured call; each invocation returns a fresh value task.
        _directory.GetActivationAddress(_grain).Returns(_ => ValueTask.FromResult(address));
#pragma warning restore CA2012
    }

    protected Task<ObserverLag> Query(string eventSequenceId = "") => ObserverLag.GetObserverLag(Store, Namespace, eventSequenceId, Id, _storage, _grainFactory, _dormant);
}

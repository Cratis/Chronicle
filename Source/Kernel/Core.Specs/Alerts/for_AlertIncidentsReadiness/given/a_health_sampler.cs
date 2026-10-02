// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.given;

public class a_health_sampler : Specification
{
    protected IStorage _storage;
    protected IGrainFactory _factory;
    protected IObserver _observer;
    protected AlertIncidentsReadiness _readiness;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _factory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _factory.GetGrain<IObserver>(AlertIncidentsReactor.ObserverKey.ToString()).Returns(_observer);
        _observer.GetState().Returns(new ObserverState() with { RunningState = ObserverRunningState.Active, NextEventSequenceNumber = 11UL });
        var ns = _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default);
        ns.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns(10UL);
        ns.FailedPartitions.GetFor(AlertIncidentsReactor.ObserverKey.ObserverId).Returns(new FailedPartitions());
        _readiness = new(_factory, _storage);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_one_of_two_clients_is_cancelled : for_Observers.given.all_dependencies
{
    readonly Storage.InMemory.Observation.ObserverDefinitionsStorage _definitions = new();
    readonly Storage.InMemory.Observation.ObserverStateStorage _states = new();
    readonly CancellationTokenSource _cancellation = new();
    readonly List<ObserverInformation[]> _remainingEmissions = [];
    IDisposable _cancelled;
    IDisposable _remaining;
    bool _cancelledCompleted;
    bool _remainingCompleted;

    void Establish()
    {
        _eventStoreStorage.Observers.Returns(_definitions);
        _namespaceStorage.Observers.Returns(_states);
        _cancelled = _observers.ObserveObservers(
            new AllObserversRequest { EventStore = "store", Namespace = "first-namespace" },
            new CallContext(new CallOptions(cancellationToken: _cancellation.Token)))
            .Subscribe(_ => { }, () => _cancelledCompleted = true);
        _remaining = _observers.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = "second-namespace" })
            .Subscribe(observers => _remainingEmissions.Add(observers.ToArray()), () => _remainingCompleted = true);
    }

    async Task Because()
    {
        await _cancellation.CancelAsync();
        await _definitions.Save(new ObserverDefinition("new-observer", [], EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true));
    }

    [Fact] void should_complete_only_the_cancelled_call() => _cancelledCompleted.ShouldBeTrue();
    [Fact] void should_not_complete_the_remaining_call() => _remainingCompleted.ShouldBeFalse();
    [Fact] void should_deliver_definition_changes_to_the_remaining_client() => _remainingEmissions[^1].Single().Id.ShouldEqual("new-observer");

    void Destroy()
    {
        _cancelled.Dispose();
        _remaining.Dispose();
        _cancellation.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}

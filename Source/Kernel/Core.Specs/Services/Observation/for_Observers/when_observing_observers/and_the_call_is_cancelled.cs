// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_the_call_is_cancelled : for_Observers.given.all_dependencies
{
    readonly Subject<IEnumerable<ObserverState>> _states = new();
    readonly Subject<IEnumerable<ObserverDefinition>> _definitions = new();
    readonly CancellationTokenSource _cancellation = new();
    bool _completed;
    IDisposable _subscription;

    void Establish()
    {
        _observerStateStorage.ObserveAll().Returns(_states);
        _observerDefinitionsStorage.ObserveAll().Returns(_definitions);
        _subscription = _observers.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = "namespace" }, new CallContext(new CallOptions(cancellationToken: _cancellation.Token)))
            .Subscribe(_ => { }, () => _completed = true);
    }

    void Because() => _cancellation.Cancel();

    [Fact] void should_complete_the_call() => _completed.ShouldBeTrue();
    [Fact] void should_unsubscribe_from_states() => _states.HasObservers.ShouldBeFalse();
    [Fact] void should_unsubscribe_from_definitions() => _definitions.HasObservers.ShouldBeFalse();

    void Destroy()
    {
        _subscription.Dispose();
        _cancellation.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}

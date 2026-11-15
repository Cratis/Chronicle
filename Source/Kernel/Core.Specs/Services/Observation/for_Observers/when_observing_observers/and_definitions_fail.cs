// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_definitions_fail : for_Observers.given.all_dependencies
{
    readonly Subject<IEnumerable<ObserverState>> _states = new();
    readonly Subject<IEnumerable<ObserverDefinition>> _definitions = new();
    readonly InvalidOperationException _failure = new("definitions failed");
    Exception _error;
    IDisposable _subscription;

    void Establish()
    {
        _observerStateStorage.ObserveAll().Returns(_states);
        _observerDefinitionsStorage.ObserveAll().Returns(_definitions);
        _subscription = _observers.ObserveObservers(new AllObserversRequest { EventStore = "store", Namespace = "namespace" })
            .Subscribe(_ => { }, error => _error = error);
    }

    void Because() => _definitions.OnError(_failure);

    [Fact] void should_forward_the_error() => _error.ShouldEqual(_failure);
    [Fact] void should_unsubscribe_from_states() => _states.HasObservers.ShouldBeFalse();

    void Destroy()
    {
        _subscription.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}

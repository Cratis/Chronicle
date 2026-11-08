// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers.given;

public class an_observed_state_stream : for_Observers.given.all_dependencies
{
    protected readonly DateTimeOffset _initialTimestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected readonly DateTimeOffset _changedTimestamp = new(2026, 1, 2, 3, 5, 6, TimeSpan.Zero);
    protected Subject<IEnumerable<ObserverState>> _states = new();
    protected BehaviorSubject<IEnumerable<ObserverDefinition>> _definitions = new([Definition("first"), Definition("second")]);
    protected List<ObserverInformation[]> _emitted = [];
    readonly object _emissionLock = new();
    TaskCompletionSource _nextEmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IDisposable _subscription;

    protected int TimestampQueries => _observerStateStorage.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IObserverStateStorage.GetRunningStatesChangedAt));

    protected int DefinitionReads => _observerDefinitionsStorage.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IObserverDefinitionsStorage.GetAll));

    protected void EmitDefinitions(params string[] ids) => _definitions.OnNext(ids.Select(Definition).ToArray());

    void Establish()
    {
        _observerStateStorage.ObserveAll().Returns(_states);
        _observerDefinitionsStorage.ObserveAll().Returns(_definitions);
        _subscription = _observers.ObserveObservers(new AllObserversRequest { EventStore = "some-event-store", Namespace = "some-namespace" })
            .Subscribe(observers =>
            {
                lock (_emissionLock)
                {
                    _emitted.Add(observers.ToArray());
                    _nextEmission.SetResult();
                    _nextEmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            });
    }

    protected Task Emit(params (string Id, ObserverRunningState RunningState, ulong LastHandled)[] states)
    {
        _states.OnNext(states.Select(_ => new ObserverState() with
        {
            Identifier = _.Id,
            RunningState = _.RunningState,
            LastHandledEventSequenceNumber = _.LastHandled
        }).ToArray());
        return Task.CompletedTask;
    }

    protected async Task EmittedCount(int count)
    {
        while (true)
        {
            Task next;
            lock (_emissionLock)
            {
                if (_emitted.Count >= count) return;
                next = _nextEmission.Task;
            }
            await next.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    protected static IReadOnlyDictionary<ObserverId, DateTimeOffset> Timestamps(DateTimeOffset timestamp) => new Dictionary<ObserverId, DateTimeOffset>
    {
        [new("first")] = timestamp,
        [new("second")] = timestamp
    };

    void Destroy()
    {
        _subscription.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }

    protected static ObserverDefinition Definition(string id) => new(
        id,
        [],
        EventSequenceId.Log,
        Concepts.Observation.ObserverType.Reactor,
        Concepts.Observation.ObserverOwner.Client,
        false);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_reducer_across_event_sources : Specification
{
    EventStoreForTesting _store;
    ISubject<IEnumerable<Tally>> _observation;
    IEnumerable<Tally> _result;
    IDisposable _subscription;
    TaskCompletionSource _received;

    async Task Establish()
    {
        _store = new();
        _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = EventSourceId.New();
        await _store.EventLog.Append(first, new Tallied());
        await _store.EventLog.Append(first, new Tallied());
        await _store.EventLog.Append(EventSourceId.New(), new Tallied());
    }

    async Task Because()
    {
        _observation = _store.ReadModels.Observe<Tally>();
        _subscription = _observation.Subscribe(
            set =>
            {
                _result = set;
                _received.TrySetResult();
            },
            error => _received.TrySetException(error));
        await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy()
    {
        _subscription.Dispose();
        ((IDisposable)_observation).Dispose();
        _store.Dispose();
    }

    [Fact] void should_reduce_each_source_independently() => _result.Select(row => row.Count).ShouldContainOnly(2, 1);
}

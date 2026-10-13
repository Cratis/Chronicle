// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class and_appends_arrive_during_a_rebuild : given.a_collection
{
    TaskCompletionSource _snapshotTaken;
    TaskCompletionSource _releaseSnapshot;
    TaskCompletionSource _finalEmission;
    int _reads;

    void Establish()
    {
        _snapshotTaken = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseSnapshot = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _finalEmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var log = Substitute.For<IEventSequence>();
        log.AppendOperations.Returns(_store.EventLog.AppendOperations);
        log.GetFromSequenceNumber(EventSequenceNumber.First).Returns(_ => ReadSnapshot());
        _observation = new ReadModelsForTesting(_store.ReadModels, log).Observe<ObservedModule>();
        Subscribe();
        using var finalSubscription = _observation.Subscribe(
            set =>
            {
                if (set.Count() == 4) _finalEmission.TrySetResult();
            },
            error => _finalEmission.TrySetException(error));
        await WaitForEmission();
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
        await _snapshotTaken.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Third"));
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Fourth"));
        _releaseSnapshot.TrySetResult();
        await _finalEmission.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    async Task<IImmutableList<AppendedEvent>> ReadSnapshot()
    {
        var snapshot = await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        if (Interlocked.Increment(ref _reads) == 2)
        {
            _snapshotTaken.TrySetResult();
            await _releaseSnapshot.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        return snapshot;
    }

    void Destroy() => _releaseSnapshot.TrySetResult();

    [Fact] void should_emit_the_final_state_of_every_append() => _emissions[^1].Select(row => row.Name).ShouldContainOnly("First", "Second", "Third", "Fourth");
    [Fact] void should_emit_the_changed_snapshot_before_the_queued_appends() => _emissions[1].Select(row => row.Name).ShouldContainOnly("First", "Second");
}

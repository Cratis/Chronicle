// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_state_persistence_fails_after_counts_are_written : for_Observer.given.an_observer_coalescing_writes
{
    readonly IStorage<ObserverState> _heldStorage = Substitute.For<IStorage<ObserverState>, IStorageStats>();
    Exception _error;
    int _increments;

    /// <summary>
    /// Initializes a new instance of the <see cref="and_state_persistence_fails_after_counts_are_written"/> class.
    /// </summary>
    public and_state_persistence_fails_after_counts_are_written()
    {
        _heldStorage.State = ObserverState.Empty;
        ((IStorageStats)_heldStorage).Stats.Returns(new TestStorageStats());
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _heldStorage : null!;
    }

    async Task Establish()
    {
        _observerHandledCountsStorage.Increment(_observerId, Arg.Any<IReadOnlyCollection<KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>>>())
            .Returns(_ =>
            {
                _increments++;
                return Task.CompletedTask;
            });
        var step = JobStepId.New();
        await _observer.ReportHandledEventsBatch(step, [new HandledEventsReport("partition", new Dictionary<EventTypeId, EventCount> { ["event"] = 3UL }, step, 0UL)]);
        _heldStorage.WriteStateAsync().Returns(_ => Task.FromException(new InvalidOperationException("state write failed")));
    }

    async Task Because()
    {
        _error = await Catch.Exception(_observer.RunWatchdogAsync);
        _heldStorage.WriteStateAsync().Returns(Task.CompletedTask);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_surface_the_state_write_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_restore_counts_that_were_already_written() => _increments.ShouldEqual(1);
    [Fact] void should_keep_the_applied_running_total() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(3UL);
}

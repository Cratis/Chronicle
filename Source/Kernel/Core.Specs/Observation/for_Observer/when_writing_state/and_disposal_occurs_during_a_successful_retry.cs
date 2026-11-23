// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_a_successful_retry : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _retry = new();
    int _attemptsBefore;
    bool _stillJoined;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () =>
        {
            _writes.BeforeWrite = () => _retry.Invoke();
            return Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        };
        var writing = _observer.Unsubscribe();
        try
        {
            await _retry.WaitForStart();
            _observer.Dispose();
            _stillJoined = !writing.IsCompleted;
        }
        finally
        {
            _retry.Complete(true);
        }
        await writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_remain_joined_until_the_retry_finishes() => _stillJoined.ShouldBeTrue();
    [Fact] void should_let_the_issued_retry_commit() => _writes.Stats.Writes.ShouldEqual(1);
    [Fact] void should_attempt_only_one_retry() => _writes.Attempts.ShouldEqual(_attemptsBefore + 2);
    [Fact] void should_not_save_the_pending_timestamp() => ShouldNotSaveTimestamp();
}

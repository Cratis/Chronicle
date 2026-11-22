// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_the_retry : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _retry = new();
    Exception? _error;
    ObserverKeyDoesNotMatchDefinition _failure;
    int _readsBeforeRetry;
    int _attemptsBefore;

    async Task Because()
    {
        _failure = new(_observerKey, EventSequenceId.Log);
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () =>
        {
            _writes.BeforeWrite = () => _retry.Invoke();
            return Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        };
        var writing = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
        try
        {
            await _retry.WaitForStart();
            _readsBeforeRetry = _definition.Stats.Reads;
            _observer.Dispose();
        }
        finally
        {
            _retry.Fail(_failure);
        }
        _error = await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_not_reload_the_definition_again() => _definition.Stats.Reads.ShouldEqual(_readsBeforeRetry);
    [Fact] void should_attempt_only_one_retry() => _writes.Attempts.ShouldEqual(_attemptsBefore + 2);
    [Fact] void should_preserve_the_real_storage_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_not_save_a_timestamp() => ShouldNotSaveTimestamp();
}

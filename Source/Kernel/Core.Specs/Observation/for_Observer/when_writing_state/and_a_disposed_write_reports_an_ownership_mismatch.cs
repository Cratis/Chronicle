// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_a_disposed_write_reports_an_ownership_mismatch : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _write = new();
    Exception? _error;
    ObserverKeyDoesNotMatchDefinition _failure;
    int _readsBefore;
    int _attemptsBefore;

    async Task Because()
    {
        _failure = new(_observerKey, EventSequenceId.Log);
        _readsBefore = _definition.Stats.Reads;
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = _write.Invoke;
        var writing = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
        try
        {
            await _write.WaitForStart();
            _observer.Dispose();
        }
        finally
        {
            _write.Fail(_failure);
        }
        _error = await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_not_reload_the_definition() => _definition.Stats.Reads.ShouldEqual(_readsBefore);
    [Fact] void should_not_claim_ownership() => _eventStoreNamespaceStorage.Observers.DidNotReceive().ClaimOwnership(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>());
    [Fact] void should_not_retry() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_preserve_the_real_storage_failure() => _error.ShouldEqual(_failure);
}

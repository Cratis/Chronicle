// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_cancellation_cannot_read_definitions : given.a_rebuild_cancelled_outside_the_worker
{
    void Establish() => _eventStoreStorage.Constraints.GetDefinitions().Returns(Task.FromException<IEnumerable<IConstraintDefinition>>(new IOException("Store unavailable")));

    async Task Because()
    {
        await CancelOutsideWorker();
        await Tick();
    }

    [Fact] void should_not_fail_cancellation() => _cancellationError.ShouldBeNull();
    [Fact] async Task should_keep_the_unconfirmed_attempt_excluded_from_repairs() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_keep_the_durable_recovery_boundary() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldNotBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_the_mutation_and_the_closing_epoch_write_fail : given.a_redaction_with_scripted_state_writes
{
    void Establish()
    {
        MutationFails();
        _writes.FailingWrites.Add(2);
    }

    Task Because() => Redact();

    [Fact] void should_surface_the_mutation_failure() => _exception.ShouldBeOfExactType<given.SimulatedStorageError>();
    [Fact] void should_log_a_warning() => _logger.Warnings.Count().ShouldEqual(1);
    [Fact] void should_close_the_in_memory_epoch() => _stateStorage.State.MutationEpoch.IsMutating.ShouldBeFalse();
}

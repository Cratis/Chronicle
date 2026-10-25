// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_the_mutation_fails : given.a_redaction_with_scripted_state_writes
{
    void Establish() => MutationFails();

    Task Because() => Redact();

    [Fact] void should_surface_the_mutation_failure() => _exception.ShouldBeOfExactType<given.SimulatedStorageError>();
    [Fact] void should_not_log_a_warning() => _logger.Warnings.ShouldBeEmpty();
    [Fact] void should_persist_a_closed_epoch() => _stateStorage.State.MutationEpoch.IsMutating.ShouldBeFalse();
    [Fact] void should_have_attempted_the_closing_write() => _writes.Attempted.ShouldEqual(2);
}

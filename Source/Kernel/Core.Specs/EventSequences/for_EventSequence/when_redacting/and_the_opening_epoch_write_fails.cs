// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_the_opening_epoch_write_fails : given.a_redaction_with_scripted_state_writes
{
    void Establish() => _writes.FailingWrites.Add(1);

    Task Because() => Redact();

    [Fact] void should_surface_the_write_failure() => _exception.ShouldBeOfExactType<given.SimulatedStateWriteError>();
    [Fact] void should_restore_the_settled_epoch() => _stateStorage.State.MutationEpoch.Value.ShouldEqual(0UL);
    [Fact] void should_not_run_the_mutation() => _eventSequenceStorage.DidNotReceiveWithAnyArgs().Redact(
        Arg.Any<EventSequenceNumber>(),
        Arg.Any<RedactionReason>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<IEnumerable<IdentityId>>(),
        Arg.Any<DateTimeOffset>());
}

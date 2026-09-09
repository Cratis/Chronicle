// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_resolving_begin_registration_conflicts : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _resumed;
    EventSequenceMutationBeginResult _definitionConflict;
    EventSequenceMutationBeginResult _sameIdDifferentTarget;
    EventSequenceMutationBeginResult _malformedDefinitionConflict;
    EventSequenceMutationBeginResult _absentMalformedRequest;
    EventSequenceMutationBeginResult _afterAbsentMalformedRequest;
    EventSequenceMutationBeginResult _invalidId;
    EventSequenceMutationBeginResult _afterInvalidId;
    EventSequenceMutationBeginResult _busy;
    EventSequenceMutationBeginResult _independent;

    async Task Because()
    {
        await _registry.Begin(_request, _proposedTarget);
        _resumed = await _registry.Begin(_request, new(100UL, 101UL, 1UL));
        _definitionConflict = await _registry.Begin(_request with { Command = new("different", "different-hash") }, _proposedTarget);
        _sameIdDifferentTarget = await _registry.Begin(
            _request with { TargetSequence = Identity("another-target") },
            _proposedTarget);
        _malformedDefinitionConflict = await _registry.Begin(_request with { Command = null! }, _proposedTarget);
        var malformedTarget = Identity("malformed-target");
        _absentMalformedRequest = await _registry.Begin(Request(malformedTarget) with { Command = null! }, _proposedTarget);
        _afterAbsentMalformedRequest = await _registry.Begin(Request(malformedTarget), _proposedTarget);
        var unregisteredTarget = Identity("unregistered-target");
        _invalidId = await _registry.Begin(
            Request(unregisteredTarget) with { Id = Guid.NewGuid() },
            _proposedTarget);
        _afterInvalidId = await _registry.Begin(Request(unregisteredTarget), _proposedTarget);
        _busy = await _registry.Begin(Request(_target, originSequenceNumber: 43), _proposedTarget);
        _independent = await _registry.Begin(Request(Identity("independent-target")), _proposedTarget);
    }

    [Fact] void should_resume_the_exact_request() => _resumed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);
    [Fact] void should_ignore_a_new_target_for_the_exact_request() => _resumed.Active!.Target.ShouldEqual(_proposedTarget);
    [Fact] void should_reject_a_nonexact_request() => _definitionConflict.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_permanently_reject_a_malformed_nonexact_request_for_an_existing_id() => _malformedDefinitionConflict.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_permanently_bind_an_id_across_target_sequences() => _sameIdDifferentTarget.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_reject_a_malformed_request_for_an_absent_id() => (_absentMalformedRequest.Outcome == EventSequenceMutationBeginOutcome.Invalid && _absentMalformedRequest.Validation!.Error == EventSequenceMutationValidationError.InvalidCommand).ShouldBeTrue();
    [Fact] void should_not_write_a_registration_for_a_malformed_absent_request() => _afterAbsentMalformedRequest.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_reject_a_nondeterministic_unregistered_id() => (_invalidId.Outcome == EventSequenceMutationBeginOutcome.Invalid && _invalidId.Validation!.Error == EventSequenceMutationValidationError.InvalidId).ShouldBeTrue();
    [Fact] void should_report_only_the_fixed_id_field_for_a_nondeterministic_id() => _invalidId.Validation!.Field.ShouldEqual(nameof(EventSequenceMutationRequest.Id));
    [Fact] void should_not_write_a_registration_for_a_nondeterministic_id() => _afterInvalidId.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_report_the_exact_busy_outcome() => (_busy.Outcome == EventSequenceMutationBeginOutcome.MutationAlreadyInProgress && _busy.Error == EventSequenceMutationRegistryError.MutationAlreadyInProgress).ShouldBeTrue();
    [Fact] void should_report_only_the_active_id_when_the_target_is_busy() => _busy.ConflictingMutationId.ShouldEqual(_request.Id);
    [Fact] void should_reserve_an_independent_target() => _independent.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
}

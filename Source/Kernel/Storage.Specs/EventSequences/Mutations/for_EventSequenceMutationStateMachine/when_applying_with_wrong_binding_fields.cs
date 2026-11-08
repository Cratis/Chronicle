// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_with_wrong_binding_fields : given.a_valid_mutation_state
{
    (EventSequenceMutationTransitionOutcome Outcome, EventSequenceMutationValidationError Error, EventSequenceMutation? Mutation, EventSequenceMutationStateToken? Token)[] _results;

    void Because()
    {
        var otherStore = _scope with { EventStore = "other-store" };
        var otherNamespace = _scope with { Namespace = "other-namespace" };
        var wrongTarget = EventSequenceMutationIdentity.TryCreate("other-target").Identity!;
        var wrongId = _active with { Definition = _definition with { Request = _request with { Id = Guid.Parse("10112233-4455-6677-8899-aabbccddeeff") } } };
        var wrongOrdinal = _active with { Ordinal = 8L };
        var wrongDigest = _active with { Definition = _definition with { DefinitionDigestV1 = new EventSequenceMutationDefinitionDigestV1(Enumerable.Repeat((byte)0x5a, 32).ToArray()) } };
        var wrongTargetKey = _active with { Definition = _definition with { Request = _request with { TargetSequence = wrongTarget } } };

        _results =
        [
            Apply(UncheckedToken(otherStore, _active)),
            Apply(UncheckedToken(otherNamespace, _active)),
            Apply(UncheckedToken(_scope, wrongId)),
            Apply(UncheckedToken(_scope, wrongOrdinal)),
            Apply(UncheckedToken(_scope, wrongDigest)),
            Apply(UncheckedToken(_scope, wrongTargetKey))
        ];
    }

    [Fact] void should_cover_event_store_namespace_id_ordinal_digest_and_target_key() => _results.Length.ShouldEqual(6);
    [Fact] void should_report_valid_binding_mismatches_as_conflicts() => _results.Take(5).All(_ => _.Outcome == EventSequenceMutationTransitionOutcome.Conflict && _.Error == EventSequenceMutationValidationError.None).ShouldBeTrue();
    [Fact] void should_report_the_scope_inconsistent_target_key_as_invalid() => _results[5].Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Invalid);
    [Fact] void should_report_the_typed_identity_error_for_the_target_key() => _results[5].Error.ShouldEqual(EventSequenceMutationValidationError.InvalidIdentity);
    [Fact] void should_return_the_current_state_for_every_conflict() => _results.Take(5).All(_ => ReferenceEquals(_.Mutation, _active)).ShouldBeTrue();
    [Fact] void should_not_return_a_mutation_for_the_invalid_target_key() => _results[5].Mutation.ShouldBeNull();
    [Fact] void should_never_return_a_successor_token() => _results.All(_ => _.Token is null).ShouldBeTrue();

    (EventSequenceMutationTransitionOutcome Outcome, EventSequenceMutationValidationError Error, EventSequenceMutation? Mutation, EventSequenceMutationStateToken? Token) Apply(EventSequenceMutationStateToken token)
    {
        var result = EventSequenceMutationStateMachine.Apply(_scope, _active, EventSequenceMutationTransition.BeginApplying, token);
        return (result.Outcome, result.Validation.Error, result.Mutation, result.Token);
    }
}

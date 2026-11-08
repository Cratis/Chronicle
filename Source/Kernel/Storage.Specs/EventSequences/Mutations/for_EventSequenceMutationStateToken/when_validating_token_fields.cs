// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using context = Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine.given.a_valid_mutation_state;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateToken;

public class when_validating_token_fields : context
{
    EventSequenceMutationValidationResult _valid;
    EventSequenceMutationValidationResult[] _invalid;

    void Because()
    {
        _valid = EventSequenceMutationValidator.ValidateToken(_token);
        var keylessIdentity = IdentityWithKey(_targetIdentity.Display, default);
        _invalid =
        [
            EventSequenceMutationValidator.ValidateToken(null),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(EventSequenceKey.NotSet, _active)),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { Definition = _definition with { Request = _request with { TargetSequence = keylessIdentity } } })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { Definition = _definition with { Request = _request with { Id = null! } } })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { Ordinal = null! })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { Definition = _definition with { DefinitionDigestV1 = null! } })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { StateVersion = null! })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { Phase = (EventSequenceMutationPhase)int.MaxValue })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope, _active with { RepairState = EventSequenceMutationRepairState.Accepted })),
            EventSequenceMutationValidator.ValidateToken(UncheckedToken(_scope with { EventSequenceId = "another" }, _active))
        ];
    }

    [Fact] void should_accept_the_complete_valid_token() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_every_malformed_or_inconsistent_token() => _invalid.All(_ => !_.IsValid).ShouldBeTrue();

    static EventSequenceMutationIdentity IdentityWithKey(string display, EventSequenceIdentityKey key) =>
        (EventSequenceMutationIdentity)typeof(EventSequenceMutationIdentity)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 2)
            .Invoke([display, key]);
}

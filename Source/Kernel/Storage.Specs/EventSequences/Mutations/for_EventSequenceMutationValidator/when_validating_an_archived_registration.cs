// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator;

public class when_validating_an_archived_registration : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _valid;
    EventSequenceMutationValidationResult[] _invalid;

    void Because()
    {
        var registration = new EventSequenceMutationRegistration(
            _definition,
            EventSequenceMutationRegistryLifecycle.Archived,
            _history.Ordinal,
            _history.TerminalWitness);
        _valid = EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history);

        var substitutedDefinition = DefinitionFor(_request with
        {
            Command = _request.Command with { Payload = "{\"name\":\"Grace\"}" }
        });
        var substitutedHistory = _history with
        {
            TerminalWitness = _history.TerminalWitness with
            {
                DefinitionDigestV1 = substitutedDefinition.DefinitionDigestV1
            }
        };
        substitutedHistory = WithValidReceiptDigest(substitutedHistory);

        _invalid =
        [
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Lifecycle = EventSequenceMutationRegistryLifecycle.Bound, TerminalWitness = null }, _history),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Ordinal = _history.Ordinal.Value + 1 }, _history),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { TerminalWitness = substitutedHistory.TerminalWitness }, substitutedHistory),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Id = Guid.NewGuid() }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Origin = _history.Origin with { SequenceNumber = 2UL } }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Kind = EventSequenceMutationKind.PointRedaction }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { CommandHash = "other" }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Target = new(20UL, 23UL, 3UL) })
        ];
    }

    [Fact] void should_accept_the_exact_registration_and_history_lineage() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_every_unrelated_or_substituted_lineage() => _invalid.All(_ => !_.IsValid).ShouldBeTrue();
}

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

        var changedOrigin = DefinitionFor(_request with { Origin = _request.Origin with { SequenceNumber = 2UL } });
        var changedOriginHistory = WithValidReceiptDigest(_history with { TerminalWitness = _history.TerminalWitness with { DefinitionDigestV1 = changedOrigin.DefinitionDigestV1 } });
        var changedCommandHash = DefinitionFor(_request with { Command = _request.Command with { Hash = "other" } });
        var changedCommandHashHistory = WithValidReceiptDigest(_history with { TerminalWitness = _history.TerminalWitness with { DefinitionDigestV1 = changedCommandHash.DefinitionDigestV1 } });

        _invalid =
        [
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Lifecycle = EventSequenceMutationRegistryLifecycle.Bound, TerminalWitness = null }, _history),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Ordinal = _history.Ordinal.Value + 1 }, _history),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { TerminalWitness = substitutedHistory.TerminalWitness }, substitutedHistory),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Id = Guid.NewGuid() }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Definition = changedOrigin }, _history with { TerminalWitness = changedOriginHistory.TerminalWitness }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Kind = EventSequenceMutationKind.PointRedaction }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration with { Definition = changedCommandHash }, _history with { TerminalWitness = changedCommandHashHistory.TerminalWitness }),
            EventSequenceMutationValidator.ValidateArchivedRegistration(_scope, registration, _history with { Target = new(20UL, 23UL, 3UL) })
        ];
    }

    [Fact] void should_accept_the_exact_registration_and_history_lineage() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_every_unrelated_or_substituted_lineage() => _invalid.All(_ => !_.IsValid).ShouldBeTrue();
}

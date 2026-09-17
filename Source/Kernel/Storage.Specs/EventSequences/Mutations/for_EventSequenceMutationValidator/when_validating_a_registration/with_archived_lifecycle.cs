// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_registration;

public class with_archived_lifecycle : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _valid;
    EventSequenceMutationValidationResult[] _invalid;

    void Because()
    {
        _valid = Validate(42L, _witness);
        _invalid =
        [
            Validate(null, _witness),
            Validate(0L, _witness),
            Validate(-1L, _witness),
            Validate(42L, null),
            Validate(42L, _witness with { FinalStateVersion = null! }),
            Validate(42L, _witness with { FinalStateVersion = EventSequenceMutationStateVersion.NotSet }),
            Validate(42L, _witness with { FinalStateVersion = -1L }),
            Validate(42L, _witness with { DefinitionDigestV1 = null! }),
            Validate(42L, _witness with { DefinitionDigestV1 = new EventSequenceMutationDefinitionDigestV1(new byte[32]) }),
            Validate(42L, _witness with { ReceiptDigestV1 = null! })
        ];
    }

    [Fact] void should_accept_only_positive_ordinal_and_complete_matching_witness() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_every_other_field_matrix() => _invalid.All(_ => _.Error == EventSequenceMutationValidationError.InvalidRegistration).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(
        EventSequenceMutationOrdinal? ordinal,
        EventSequenceMutationTerminalWitness? witness) =>
        EventSequenceMutationValidator.ValidateRegistration(
            _scope,
            new(_definition, EventSequenceMutationRegistryLifecycle.Archived, ordinal, witness));
}

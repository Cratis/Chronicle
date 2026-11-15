// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_registration;

public class with_claimed_lifecycle : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _valid;
    EventSequenceMutationValidationResult[] _invalid;

    void Because()
    {
        _valid = Validate(null, null);
        _invalid =
        [
            Validate(42L, null),
            Validate(null, _witness),
            Validate(0L, null),
            Validate(-1L, null),
            Validate(42L, _witness)
        ];
    }

    [Fact] void should_accept_only_null_ordinal_and_null_witness() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_every_other_field_matrix() => _invalid.All(_ => _.Error == EventSequenceMutationValidationError.InvalidRegistration).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(
        Concepts.EventSequences.Mutations.EventSequenceMutationOrdinal? ordinal,
        EventSequenceMutationTerminalWitness? witness) =>
        EventSequenceMutationValidator.ValidateRegistration(
            _scope,
            new(_definition, EventSequenceMutationRegistryLifecycle.Claimed, ordinal, witness));
}

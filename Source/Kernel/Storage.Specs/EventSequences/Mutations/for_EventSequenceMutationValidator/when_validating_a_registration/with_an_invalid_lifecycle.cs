// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_registration;

public class with_an_invalid_lifecycle : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRegistration(_scope, new(_definition, EventSequenceMutationRegistryLifecycle.Unknown, null, null)),
        EventSequenceMutationValidator.ValidateRegistration(_scope, new(_definition, (EventSequenceMutationRegistryLifecycle)int.MaxValue, null, null))
    ];

    [Fact] void should_reject_unknown_and_undefined_lifecycles() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidEnum).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_an_active_mutation;

public class with_a_stale_definition_digest : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _result;

    void Because() => _result = EventSequenceMutationValidator.ValidateActive(
        _scope,
        _mutation with
        {
            Definition = _definition with
            {
                Request = _request with { Command = _request.Command with { Payload = "changed" } }
            }
        });

    [Fact] void should_reject_the_recomputed_digest() => _result.Error.ShouldEqual(EventSequenceMutationValidationError.InvalidDigest);
}

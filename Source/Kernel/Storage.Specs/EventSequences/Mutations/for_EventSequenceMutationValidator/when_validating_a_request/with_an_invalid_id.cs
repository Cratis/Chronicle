// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_invalid_id : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRequest(_request with { Id = null! }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Id = EventSequenceMutationId.NotSet })
    ];

    [Fact] void should_reject_every_invalid_id() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidId).ShouldBeTrue();
}

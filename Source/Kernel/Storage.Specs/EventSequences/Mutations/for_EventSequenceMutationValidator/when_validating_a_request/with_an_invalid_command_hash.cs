// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_invalid_command_hash : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRequest(_request with { Command = _request.Command with { Hash = null! } }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Command = _request.Command with { Hash = EventSequenceMutationCommandHash.NotSet } }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Command = _request.Command with { Hash = "\udc00" } })
    ];

    [Fact] void should_reject_missing_empty_and_non_strict_text() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidCommand).ShouldBeTrue();
}

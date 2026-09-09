// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_empty_command_payload : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _result;

    void Because() => _result = EventSequenceMutationValidator.ValidateRequest(_request with { Command = _request.Command with { Payload = string.Empty } });

    [Fact] void should_be_valid_strict_text() => _result.IsValid.ShouldBeTrue();
}

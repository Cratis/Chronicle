// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_definition;

public class with_an_invalid_digest : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateDefinition(_scope, _definition with { DefinitionDigestV1 = null! }),
        EventSequenceMutationValidator.ValidateDefinition(_scope, _definition with { DefinitionDigestV1 = new EventSequenceMutationDefinitionDigestV1(new byte[32]) })
    ];

    [Fact] void should_reject_missing_and_non_matching_digests() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidDigest).ShouldBeTrue();
}

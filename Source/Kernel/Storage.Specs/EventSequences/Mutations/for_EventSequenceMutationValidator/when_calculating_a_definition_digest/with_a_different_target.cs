// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_calculating_a_definition_digest;

public class with_a_different_target : given.a_mutation_validation
{
    EventSequenceMutationDefinitionDigestV1 _result;

    void Because() => _result = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _request, new(20UL, 22UL, 2UL));

    [Fact] void should_change_the_digest() => _result.ShouldNotEqual(_definition.DefinitionDigestV1);
}

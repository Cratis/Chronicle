// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_creating_a_definition;

public class with_valid_inputs : given.a_mutation_validation
{
    EventSequenceMutationDefinition _result;

    void Because() => _result = EventSequenceMutationDefinition.Create(_scope, _request, _target);

    [Fact] void should_recompute_the_definition_digest() => _result.DefinitionDigestV1.ShouldEqual(EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _request, _target));
}

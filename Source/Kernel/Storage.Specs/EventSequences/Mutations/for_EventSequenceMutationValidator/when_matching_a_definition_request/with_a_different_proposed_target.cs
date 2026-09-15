// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_matching_a_definition_request;

public class with_a_different_proposed_target : given.a_mutation_validation
{
    bool _result;

    void Because()
    {
        var definitionWithAnotherTarget = DefinitionFor(target: new(20UL, 22UL, 2UL));
        _result = definitionWithAnotherTarget.IsExactRequest(_request);
    }

    [Fact] void should_exclude_the_target_from_exact_request_comparison() => _result.ShouldBeTrue();
}

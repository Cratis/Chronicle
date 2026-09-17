// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_matching_a_registration_request;

public class with_the_exact_request : given.a_mutation_validation
{
    bool _result;

    void Because() => _result = new EventSequenceMutationRegistration(
        _definition,
        EventSequenceMutationRegistryLifecycle.Claimed,
        null,
        null).IsExactRequest(_request);

    [Fact] void should_match() => _result.ShouldBeTrue();
}

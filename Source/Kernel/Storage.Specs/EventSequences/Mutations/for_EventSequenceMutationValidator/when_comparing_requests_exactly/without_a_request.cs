// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_comparing_requests_exactly;

public class without_a_request : given.a_mutation_validation
{
    bool _result;

    void Because() => _result = _request.ExactlyEquals(null);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
}

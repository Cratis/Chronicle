// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_comparing_requests_exactly;

public class with_a_different_command_payload : given.a_mutation_validation
{
    bool _result;

    void Because() => _result = _request.ExactlyEquals(_request with { Command = _request.Command with { Payload = "{\"name\":\"ada\"}" } });

    [Fact] void should_not_be_equal_ordinally() => _result.ShouldBeFalse();
}

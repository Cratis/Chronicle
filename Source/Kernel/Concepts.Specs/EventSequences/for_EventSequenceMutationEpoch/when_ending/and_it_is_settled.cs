// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.for_EventSequenceMutationEpoch.when_ending;

public class and_it_is_settled : Specification
{
    EventSequenceMutationEpoch _result;

    void Because() => _result = new EventSequenceMutationEpoch(4).End();

    [Fact] void should_give_the_next_even_value() => _result.Value.ShouldEqual(6UL);
    [Fact] void should_not_be_mutating() => _result.IsMutating.ShouldBeFalse();
}

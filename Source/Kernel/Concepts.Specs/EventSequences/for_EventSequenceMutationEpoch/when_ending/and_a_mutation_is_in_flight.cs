// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.for_EventSequenceMutationEpoch.when_ending;

public class and_a_mutation_is_in_flight : Specification
{
    EventSequenceMutationEpoch _result;

    void Because() => _result = new EventSequenceMutationEpoch(5).End();

    [Fact] void should_give_the_next_even_value() => _result.Value.ShouldEqual(6UL);
    [Fact] void should_not_be_mutating() => _result.IsMutating.ShouldBeFalse();
}

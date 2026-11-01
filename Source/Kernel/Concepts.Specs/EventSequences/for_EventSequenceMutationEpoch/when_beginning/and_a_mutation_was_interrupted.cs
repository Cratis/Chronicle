// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.for_EventSequenceMutationEpoch.when_beginning;

public class and_a_mutation_was_interrupted : Specification
{
    EventSequenceMutationEpoch _result;

    void Because() => _result = new EventSequenceMutationEpoch(5).Begin();

    [Fact] void should_give_the_next_odd_value() => _result.Value.ShouldEqual(7UL);
    [Fact] void should_be_mutating() => _result.IsMutating.ShouldBeTrue();
}

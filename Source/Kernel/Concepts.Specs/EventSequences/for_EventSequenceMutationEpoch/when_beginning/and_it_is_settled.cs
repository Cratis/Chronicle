// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.for_EventSequenceMutationEpoch.when_beginning;

public class and_it_is_settled : Specification
{
    EventSequenceMutationEpoch _result;

    void Because() => _result = new EventSequenceMutationEpoch(4).Begin();

    [Fact] void should_give_the_next_odd_value() => _result.Value.ShouldEqual(5UL);
    [Fact] void should_be_mutating() => _result.IsMutating.ShouldBeTrue();
}

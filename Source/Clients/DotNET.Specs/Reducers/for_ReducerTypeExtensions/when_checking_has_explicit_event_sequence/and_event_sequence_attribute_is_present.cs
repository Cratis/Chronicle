// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_event_sequence_attribute_is_present : Specification
{
    record ReadModel;

    [EventSequence("my-sequence")]
    [Reducer]
    class ReducerWithEventSequenceAttribute : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(ReadModel? current) => new();
    }

    bool _result;

    void Because() => _result = typeof(ReducerWithEventSequenceAttribute).HasExplicitEventSequence();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_reducer_attribute_has_event_sequence : Specification
{
    record ReadModel;

    [Reducer(eventSequence: "my-sequence")]
    class ReducerWithExplicitEventSequenceInAttribute : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(ReadModel? current) => new();
    }

    bool _result;

    void Because() => _result = typeof(ReducerWithExplicitEventSequenceInAttribute).HasExplicitEventSequence();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}

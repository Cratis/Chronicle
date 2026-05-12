// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_no_explicit_event_sequence_is_set : Specification
{
    record ReadModel;

    [Reducer]
    class ReducerWithoutExplicitEventSequence : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(ReadModel? current) => new();
    }

    bool _result;

    void Because() => _result = typeof(ReducerWithoutExplicitEventSequence).HasExplicitEventSequence();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}

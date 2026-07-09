// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.get_target_event_source_ids;

public class with_duplicate_ids_across_failures : Specification
{
    readonly EventSourceId _firstId = EventSourceId.New();
    readonly EventSourceId _secondId = EventSourceId.New();
    EventSourceId[] _result;

    void Because()
    {
        var failure = new ReactorSideEffectFailure(
            [
                new AppendFailure([], false, [], [_firstId, _secondId]),
                new AppendFailure([], false, [], [_secondId])
            ]);

        _result = failure.GetTargetEventSourceIds().ToArray();
    }

    [Fact] void should_return_the_distinct_ids() => _result.Length.ShouldEqual(2);
    [Fact] void should_contain_the_first_id() => _result.ShouldContain(_firstId);
    [Fact] void should_contain_the_second_id() => _result.ShouldContain(_secondId);
}

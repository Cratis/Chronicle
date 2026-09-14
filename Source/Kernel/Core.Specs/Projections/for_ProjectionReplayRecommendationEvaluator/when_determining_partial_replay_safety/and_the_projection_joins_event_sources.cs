// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayRecommendationEvaluator.when_determining_partial_replay_safety;

public class and_the_projection_joins_event_sources : given.a_projection_replay_recommendation_evaluator
{
    bool _result;

    void Because()
    {
        var previous = CreateDefinition() with { AutoMap = AutoMap.Disabled };
        var current = CreateDefinition(join: new Dictionary<EventType, JoinDefinition>
        {
            [EventType1] = new("customerId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet)
        }) with { AutoMap = AutoMap.Disabled };

        _result = ProjectionReplayRecommendationEvaluator.CanPartiallyReplay(previous, current, [EventType1]);
    }

    [Fact] void should_require_full_replay() => _result.ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator.when_adding_event_types;

public class and_the_first_event_is_at_the_next_position : given.an_additive_subscription
{
    void Establish() => Context = Context with { FirstEventSequenceNumberForAddedEventTypes = Context.State.NextEventSequenceNumber };
    Task Because() => Evaluate();
    [Fact] void should_not_recommend_replay() => Recommendations.ReceivedCalls().ShouldBeEmpty();
}

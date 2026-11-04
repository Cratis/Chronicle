// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator.when_adding_event_types;

public class and_no_historical_events_exist : given.an_additive_subscription
{
    void Establish() => Context = Context with { FirstEventSequenceNumberForAddedEventTypes = EventSequenceNumber.Unavailable };
    Task Because() => Evaluate();
    [Fact] void should_not_recommend_replay() => Recommendations.ReceivedCalls().ShouldBeEmpty();
}

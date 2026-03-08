// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator;

public class when_evaluating_and_observer_is_not_replayable : Specification
{
    ReplayEvaluator _evaluator;
    ReplayEvaluationContext _context;
    bool _result;

    void Establish()
    {
        _evaluator = new ReplayEvaluator(
            Substitute.For<IGrainFactory>(),
            EventStoreName.NotSet,
            EventStoreNamespaceName.NotSet);

        var observerState = new ObserverState
        {
            Identifier = "test-observer",
        };

        var observerDefinition = new ObserverDefinition
        {
            Identifier = "test-observer",
            EventTypes = [EventType.Unknown],
            IsReplayable = false
        };

        var subscription = new ObserverSubscription(
            "test-observer",
            new("test-observer", EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log),
            [EventType.Unknown],
            typeof(object),
            SiloAddress.Zero);

        _context = new ReplayEvaluationContext(
            "test-observer",
            new("test-observer", EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log),
            observerDefinition,
            observerState,
            subscription,
            EventSequenceNumber.First,
            EventSequenceNumber.First);
    }

    async Task Because() => _result = await _evaluator.Evaluate(_context);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}

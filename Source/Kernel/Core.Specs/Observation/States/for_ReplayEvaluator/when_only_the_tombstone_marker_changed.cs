// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator;

public class when_only_the_tombstone_marker_changed : Specification
{
    IGrainFactory _grains;
    ReplayEvaluator _evaluator;
    ReplayEvaluationContext _context;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _evaluator = new(_grains, "store", "namespace");
        var type = new EventType("some-event", 1);
        var key = new Concepts.Observation.ObserverKey("observer", "store", "namespace", EventSequenceId.Log);
        var definition = new ObserverDefinition { Identifier = "observer", EventTypes = [type with { Tombstone = true }], IsReplayable = true };
        var subscription = new ObserverSubscription("observer", key, [type], typeof(object), SiloAddress.Zero);
        _context = new("observer", key, definition, new ObserverState { Identifier = "observer" }, subscription, EventSequenceNumber.First, EventSequenceNumber.First);
    }

    Task Because() => _evaluator.Evaluate(_context);

    [Fact] void should_not_request_a_replay() => _grains.DidNotReceive().GetGrain<Recommendations.IRecommendationsManager>(Arg.Any<long>(), Arg.Any<string>());
}

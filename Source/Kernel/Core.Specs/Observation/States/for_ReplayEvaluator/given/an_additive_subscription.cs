// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_ReplayEvaluator.given;

public class an_additive_subscription : Specification
{
    protected IRecommendationsManager Recommendations;
    protected ReplayEvaluator Evaluator;
    protected ReplayEvaluationContext Context;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        Recommendations = Substitute.For<IRecommendationsManager>();
        grainFactory.GetGrain<IRecommendationsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(Recommendations);
        Evaluator = new(grainFactory, EventStoreName.NotSet, EventStoreNamespaceName.NotSet);
        var key = new Concepts.Observation.ObserverKey("observer", EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
        Context = new(
            "observer",
            key,
            new ObserverDefinition { Identifier = "observer", EventTypes = [new("original", 1)], IsReplayable = true },
            new ObserverState { Identifier = "observer", NextEventSequenceNumber = 10 },
            new ObserverSubscription("observer", key, [new("original", 1), new("added", 1)], typeof(object), SiloAddress.Zero),
            20,
            20);
    }

    protected Task Evaluate() => Evaluator.Evaluate(Context);
}

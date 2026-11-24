// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.when_the_definition_changes;

public class and_replay_is_recommended : given.a_client_reactor_subscription_with_replay_on_definition_change
{
    void Establish()
    {
        _options.Value.Returns(new ChronicleOptions { Observers = new Observers { ReplayOnDefinitionChange = false } });
        _namespacesGrain.GetAll().Returns([EventStoreNamespaceName.Default]);
    }

    Task Because() => _subscription.SetDefinitionAndSubscribe(_definition);

    [Fact]
    Task should_recommend_with_the_definition_change_id() => _recommendationsProbe.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
        Arg.Any<RecommendationDescription>(),
        Arg.Is<ReplayCandidateRequest>(request => request.ChangeId.Length > 0 && request.ChangeId == DefinitionChangeIdentity.For(_definition)));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_projection_registration_has_another_incoming_version : given.an_ignored_projection_registration
{
    Task Because() => _manager.Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(
        "Some description", new() { ProjectionId = "p", IncomingVersion = "1.5.0", StoredVersion = "2.0.0" });

    [Fact] void should_keep_the_ignored_recommendation() => _recommendationStorage.DidNotReceive().Remove(Arg.Any<RecommendationId>());
}

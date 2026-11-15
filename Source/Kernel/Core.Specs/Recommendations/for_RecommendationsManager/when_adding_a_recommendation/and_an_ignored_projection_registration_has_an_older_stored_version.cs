// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_an_ignored_projection_registration_has_an_older_stored_version : given.an_ignored_projection_registration
{
    Task Because() => _manager.Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(
        "Some description", new() { ProjectionId = "p", IncomingVersion = "1.0.0", StoredVersion = "2.1.0" });

    [Fact] void should_remove_the_superseded_ignored_recommendation() => _recommendationStorage.Received(1).Remove(_ignoredId);
    [Fact] void should_initialize_a_new_recommendation() => _projectionRecommendation.Received(1).Initialize(Arg.Any<RecommendationDescription>(), Arg.Any<IgnoredProjectionRegistrationRequest>());
}

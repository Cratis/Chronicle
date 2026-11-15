// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

public class an_ignored_projection_registration : all_dependencies
{
    protected RecommendationId _ignoredId;
    protected IIgnoredProjectionRegistrationRecommendation _projectionRecommendation;
    protected IgnoredProjectionRegistrationRequest _ignoredRequest;

    void Establish()
    {
        _projectionRecommendation = Substitute.For<IIgnoredProjectionRegistrationRecommendation>();
        _silo.AddProbe(_ => _projectionRecommendation);
        _ignoredId = RecommendationId.New();
        _ignoredRequest = new IgnoredProjectionRegistrationRequest { ProjectionId = "p", IncomingVersion = "1.0.0", StoredVersion = "2.0.0" };
        _storedRecommendations.Add(new RecommendationState
        {
            Id = _ignoredId,
            Type = (RecommendationType)typeof(IIgnoredProjectionRegistrationRecommendation),
            Request = _ignoredRequest,
            IsIgnored = true
        });
    }
}

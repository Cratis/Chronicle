// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_Recommendation.given;

public class TestRecommendation<TRequest> : Recommendation<TRequest>, IGrainType
    where TRequest : class, IRecommendationRequest
{
    public Type GrainType => typeof(Recommendation<TRequest>);
}

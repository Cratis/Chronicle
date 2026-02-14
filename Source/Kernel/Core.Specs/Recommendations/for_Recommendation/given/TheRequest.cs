// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Recommendations.for_Recommendation.given;

[DerivedType("c7eed5a9-1678-4fab-8303-0e5c01cdc3ea")]
public record TheRequest(int SomeValue) : IRecommendationRequest;

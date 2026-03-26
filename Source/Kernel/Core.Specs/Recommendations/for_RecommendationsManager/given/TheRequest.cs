// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

[DerivedType("a3b7f1e9-2c5d-4e8a-b6f0-9d4c3a2e1f7b")]
public record TheRequest(int SomeValue) : IRecommendationRequest;

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Recommendations;

namespace Cratis.Chronicle.Grains.Recommendations;

/// <summary>
/// Exception that gets thrown when a recommendation does not exist.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownRecommendation"/> class.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> the recommendation wasn't found in.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the recommendation wasn't found for.</param>
/// <param name="recommendationId">The <see cref="RecommendationId"/> that wasn't found.</param>
public class UnknownRecommendation(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    RecommendationId recommendationId) : Exception($"Unknown recommendation with id {recommendationId} for namespace {@namespace} in event store {eventStore}");

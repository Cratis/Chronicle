// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations;

/// <summary>
/// Converts between <see cref="Recommendation"/> and <see cref="RecommendationState"/>.
/// </summary>
public class RecommendationConverter
{
    /// <summary>
    /// Converts a <see cref="RecommendationState"/> to a <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendationId">The recommendation identifier.</param>
    /// <param name="recommendationState">The recommendation state.</param>
    /// <returns>The recommendation entity.</returns>
    public Recommendation ToEntity(RecommendationId recommendationId, RecommendationState recommendationState)
    {
        return new Recommendation
        {
            Id = recommendationId.Value,
            Name = recommendationState.Name.Value,
            Description = recommendationState.Description.Value,
            Type = recommendationState.Type.Value,
            Occurred = recommendationState.Occurred,
            RequestJson = JsonSerializer.Serialize(recommendationState.Request)
        };
    }

    /// <summary>
    /// Converts a <see cref="Recommendation"/> to a <see cref="RecommendationState"/>.
    /// </summary>
    /// <param name="entity">The recommendation entity.</param>
    /// <returns>The recommendation state.</returns>
    public RecommendationState ToRecommendationState(Recommendation entity)
    {
        return new RecommendationState
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Type = new RecommendationType(entity.Type),
            Occurred = entity.Occurred,
            Request = JsonSerializer.Deserialize<IRecommendationRequest>(entity.RequestJson) ?? null!
        };
    }
}

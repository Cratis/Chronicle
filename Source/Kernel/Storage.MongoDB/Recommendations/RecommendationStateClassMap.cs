// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.Recommendations;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations;

/// <summary>
/// Represents the class map for <see cref="RecommendationState"/>.
/// </summary>
public class RecommendationStateClassMap : IBsonClassMapFor<RecommendationState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<RecommendationState> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);

        // Unmap the Request property to prevent MongoDB from using discriminator-based serialization.
        // The RecommendationStateSerializer handles complete serialization/deserialization
        // by resolving the concrete request type from the RecommendationType.
        classMap.UnmapMember(_ => _.Request);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Storage.Recommendations;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations;

/// <summary>
/// MongoDB specific class map for <see cref="RecommendationState"/>.
/// </summary>
public class RecommendationStateClassMap : IBsonClassMapFor<RecommendationState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<RecommendationState> classMap)
    {
        classMap.AutoMap();
        classMap.UnmapMember(_ => _.Request);
    }
}

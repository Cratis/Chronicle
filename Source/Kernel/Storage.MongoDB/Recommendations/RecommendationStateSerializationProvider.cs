// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Recommendations;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="RecommendationState"/>.
/// </summary>
/// <param name="serializer">The <see cref="RecommendationStateSerializer"/> instance.</param>
public class RecommendationStateSerializationProvider(RecommendationStateSerializer serializer) : IBsonSerializationProvider
{
    /// <inheritdoc/>
    public IBsonSerializer? GetSerializer(Type type) =>
        type == typeof(RecommendationState) ? serializer : null;
}

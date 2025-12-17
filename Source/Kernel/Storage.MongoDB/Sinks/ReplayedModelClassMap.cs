// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the mapping for <see cref="ReplayedModel"/>.
/// </summary>
public class ReplayedModelClassMap : IBsonClassMapFor<ReplayedModel>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ReplayedModel> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.ReadModel);
    }
}

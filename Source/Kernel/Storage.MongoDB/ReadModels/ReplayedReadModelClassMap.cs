// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents the mapping for <see cref="ReplayedReadModel"/>.
/// </summary>
public class ReplayedReadModelClassMap : IBsonClassMapFor<ReplayedReadModel>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ReplayedReadModel> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.ReadModel);
    }
}

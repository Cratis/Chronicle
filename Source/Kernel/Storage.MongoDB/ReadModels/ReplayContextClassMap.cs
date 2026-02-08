// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents a class map for <see cref="ReplayContext"/>.
/// </summary>
public class ReplayContextClassMap : IBsonClassMapFor<ReplayContext>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ReplayContext> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.ContainerName);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents mapping <see cref="EventStore"/> to MongoDB.
/// </summary>
public class EventStoreMap : IBsonClassMapFor<EventStore>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<EventStore> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Name);
    }
}

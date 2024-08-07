// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a class map for <see cref="Event"/>.
/// </summary>
public class EventClassMap : IBsonClassMapFor<Event>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<Event> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.SequenceNumber);
    }
}

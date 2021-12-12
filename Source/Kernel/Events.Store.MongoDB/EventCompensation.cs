// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

#nullable disable

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents the compensation of an event.
    /// </summary>
    /// <param name="Occurred">The time the compensation occurred.</param>
    /// <param name="Content">The content per event type generation.</param>
    public record EventCompensation(DateTimeOffset Occurred, Dictionary<EventGeneration, BsonDocument> Content);
}

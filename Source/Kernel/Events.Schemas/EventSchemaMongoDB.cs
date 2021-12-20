// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents the <see cref="EventSchema"/> for MongoDB storage purpose.
    /// </summary>
    public class EventSchemaMongoDB
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        [BsonId]
        public string Id
        {
            get => $"{EventType}-{Generation}";
            set
            {
            }
        }

        /// <summary>
        /// Gets the identifier part of <see cref="EventType"/>.
        /// </summary>
        public Guid EventType { get; init; } = EventTypeId.Unknown.Value;

        /// <summary>
        /// Gets the generation part of the <see cref="EventType"/>>.
        /// </summary>
        public uint Generation { get; init; } = EventGeneration.First.Value;

        /// <summary>
        /// Gets the actual schema as JSON.
        /// </summary>
        public string Schema { get; init; } = string.Empty;
    }
}

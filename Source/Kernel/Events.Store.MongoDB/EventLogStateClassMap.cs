// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents a class map for <see cref="EventLogState"/>.
    /// </summary>
    public class EventLogStateClassMap : IBsonClassMapFor<EventLogState>
    {
        /// <inheritdoc/>
        public void Configure(BsonClassMap<EventLogState> classMap)
        {
            classMap.AutoMap();
            classMap.MapIdProperty(_ => _.EventLog);
        }
    }
}

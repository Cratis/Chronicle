// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using Cratis.Events.Store.Observation;
using Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Events.Store.MongoDB.Observation
{
    /// <summary>
    /// Represents the class map for <see cref="ObserverState"/>.
    /// </summary>
    public class ObserverStateClassMap : IBsonClassMapFor<ObserverState>
    {
        /// <inheritdoc/>
        public void Configure(BsonClassMap<ObserverState> classMap)
        {
            classMap.AutoMap();
            classMap.MapIdProperty(_ => _.Id);
        }
    }
}

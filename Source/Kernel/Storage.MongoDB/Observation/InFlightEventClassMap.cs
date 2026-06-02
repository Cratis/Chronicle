// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// A class map for <see cref="InFlightEvent"/>.
/// </summary>
public class InFlightEventClassMap : IBsonClassMapFor<InFlightEvent>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<InFlightEvent> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
    }
}

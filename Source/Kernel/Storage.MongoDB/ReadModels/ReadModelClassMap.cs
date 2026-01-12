// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents the class map for <see cref="ReadModel"/>.
/// </summary>
public class ReadModelClassMap : IBsonClassMapFor<ReadModel>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ReadModel> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
        classMap.MapProperty(_ => _.ObserverType).SetDefaultValue(ReadModelObserverType.NotSet);
        classMap.MapProperty(_ => _.ObserverIdentifier).SetDefaultValue(ReadModelObserverIdentifier.Unspecified);
    }
}

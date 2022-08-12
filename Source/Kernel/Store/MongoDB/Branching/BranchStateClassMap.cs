// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Events.Store.MongoDB.Branching;

/// <summary>
/// Represents a class map for <see cref="BranchState"/>.
/// </summary>
public class BranchStateClassMap : IBsonClassMapFor<BranchState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<BranchState> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Identifier);
    }
}

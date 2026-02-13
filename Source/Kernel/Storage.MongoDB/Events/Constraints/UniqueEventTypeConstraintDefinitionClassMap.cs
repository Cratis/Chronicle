// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a class map for <see cref="UniqueEventTypeConstraintDefinition"/>.
/// </summary>
public class UniqueEventTypeConstraintDefinitionClassMap : IBsonClassMapFor<UniqueEventTypeConstraintDefinition>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<UniqueEventTypeConstraintDefinition> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Name);
        classMap.SetIsRootClass(true);
    }
}

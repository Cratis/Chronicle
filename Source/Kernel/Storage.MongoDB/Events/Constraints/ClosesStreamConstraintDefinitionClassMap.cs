// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Maps closing-event definitions into the discriminated constraint storage format.
/// </summary>
public class ClosesStreamConstraintDefinitionClassMap : IBsonClassMapFor<ClosesStreamConstraintDefinition>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ClosesStreamConstraintDefinition> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(definition => definition.Name);
        classMap.SetIsRootClass(true);
    }
}

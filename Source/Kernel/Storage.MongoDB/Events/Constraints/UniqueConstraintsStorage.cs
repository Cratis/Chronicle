// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
public class UniqueConstraintsStorage(IEventStoreNamespaceDatabase eventStoreNamespaceDatabase) : IUniqueConstraintsStorage
{
    /// <inheritdoc/>
    public Task<(bool Exists, EventSequenceNumber SequenceNumber)> Exists(ConstraintName name, UniqueConstraintValue value) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Save(ConstraintName name, EventSequenceNumber sequenceNumber, UniqueConstraintValue value)
    {
        eventStoreNamespaceDatabase.GetCollection
    }
}


/// <summary>
/// Represents
/// </summary>
/// <param name="Value"></param>
/// <param name="SequenceNumber"></param>
/// <returns></returns>
public record UniqueConstraintValueObject(UniqueConstraintValue Value, EventSequenceNumber SequenceNumber);

/// <summary>
/// Represents the mapping for <see cref="UniqueConstraintValueObject"/>.
/// </summary>
public class UniqueConstraintValueObjectClassMap : IBsonClassMapFor<UniqueConstraintValueObject>
{
    public void Define(BsonClassMap<UniqueConstraintValueObject> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Value);
    }
}

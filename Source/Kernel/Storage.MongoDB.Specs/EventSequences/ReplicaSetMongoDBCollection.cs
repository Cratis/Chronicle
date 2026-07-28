// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Collection fixture that shares a single <see cref="ReplicaSetMongoDBFixture"/> across event sequence storage specs that need transactions.
/// </summary>
[CollectionDefinition(Name)]
public class ReplicaSetMongoDBCollection : ICollectionFixture<ReplicaSetMongoDBFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "ReplicaSetMongoDB";
}

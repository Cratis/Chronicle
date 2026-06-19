// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Collection fixture that shares a single <see cref="MongoDBFixture"/> across MongoDB sink integration specs.
/// </summary>
[CollectionDefinition(Name)]
public class MongoDBCollection : ICollectionFixture<MongoDBFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "MongoDB";
}

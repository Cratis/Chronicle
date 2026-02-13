// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration;

/// <summary>
/// Collection fixture for the MongoDB integration tests.
/// </summary>
[CollectionDefinition(Name)]
public class MongoDBCollection : ICollectionFixture<ChronicleInProcessFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "MongoDB";
}

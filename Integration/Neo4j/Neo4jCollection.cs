// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Neo4j.Integration;

/// <summary>
/// Collection fixture for the Neo4j integration tests, so one server serves every specification of the run.
/// </summary>
[CollectionDefinition(Name)]
public class Neo4jCollection : ICollectionFixture<Neo4jFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Neo4j";
}

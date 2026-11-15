// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Collection fixture for the graph projection specifications.
/// </summary>
[CollectionDefinition(Name)]
public class GraphChronicleCollection : ICollectionFixture<GraphChronicleFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "ChronicleGraph";
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Collection fixture for the clustering integration tests that observe over one multiplexed stream per connection.
/// </summary>
[CollectionDefinition(Name)]
public class MultiplexedChronicleCollection : ICollectionFixture<MultiplexedClusteringFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Chronicle Clustering Multiplexed";
}

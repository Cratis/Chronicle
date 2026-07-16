// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Collection fixture for the Chronicle clustering integration tests.
/// </summary>
[CollectionDefinition(Name)]
public class ChronicleCollection : ICollectionFixture<ClusteringFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Chronicle Clustering";
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.Integration;

/// <summary>
/// Collection definition for clustering tests.
/// </summary>
[CollectionDefinition(Name)]
public class ClusterCollection : ICollectionFixture<ClusteringFixture>
{
    /// <summary>
    /// Name of the cluster collection.
    /// </summary>
    public const string Name = "Cluster";
}

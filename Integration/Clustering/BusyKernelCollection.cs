// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// The collection sharing one <see cref="BusyKernelClusteringFixture"/>.
/// </summary>
[CollectionDefinition(Name)]
public class BusyKernelCollection : ICollectionFixture<BusyKernelClusteringFixture>
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "Chronicle Clustering Busy Kernel";
}

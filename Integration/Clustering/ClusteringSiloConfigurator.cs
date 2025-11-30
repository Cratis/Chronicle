// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.Concepts;
using Orleans.TestingHost;

namespace Cratis.Chronicle.Clustering.Integration;

/// <summary>
/// Silo configuration for clustering tests.
/// </summary>
public class ClusteringSiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc/>
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddConceptSerializer();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.Concepts;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace Cratis.Chronicle.Clustering.Integration;

/// <summary>
/// Client configuration for clustering tests.
/// </summary>
public class ClusteringClientConfigurator : IClientBuilderConfigurator
{
    /// <inheritdoc/>
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddConceptSerializer();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
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
        var jsonOptions = ClusteringSerializationConfiguration.CreateJsonSerializerOptions();

        siloBuilder.Services.AddSingleton(jsonOptions);
        siloBuilder.Services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
                ClusteringSerializationConfiguration.IsSerializableType,
                jsonOptions));
    }
}

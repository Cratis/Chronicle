// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
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
        var jsonOptions = ClusteringSerializationConfiguration.CreateJsonSerializerOptions();

        clientBuilder.Services.AddSingleton(jsonOptions);
        clientBuilder.Services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
                ClusteringSerializationConfiguration.IsSerializableType,
                jsonOptions));
    }
}

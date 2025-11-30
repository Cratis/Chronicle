// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Applications.Orleans.Concepts;
using Cratis.Json;
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
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new EnumConverterFactory(),
                new EnumerableConceptAsJsonConverterFactory(),
                new ConceptAsJsonConverterFactory(),
                new DateOnlyJsonConverter(),
                new TimeOnlyJsonConverter()
            }
        };

        siloBuilder.Services.AddSingleton(jsonOptions);
        siloBuilder.Services.AddConceptSerializer();
        siloBuilder.Services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
                type => type.Namespace?.StartsWith("Cratis") ?? false,
                jsonOptions));
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Properties;
using Aksio.Json;
using Orleans.Serialization;

namespace Aksio.Cratis.Kernel.Server.Serialization;

/// <summary>
/// Extension methods for configuring serialization.
/// </summary>
public static class SerializationConfigurationExtensions
{
    /// <summary>
    /// Configure serialization for Orleans.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder ConfigureSerialization(this ISiloBuilder siloBuilder)
    {
        siloBuilder.ConfigureServices(Configure);

        return siloBuilder;
    }

    static void Configure(this IServiceCollection services)
    {
        var options = new JsonSerializerOptions(Globals.JsonSerializerOptions);
        options.Converters.Add(new TypeJsonConverter());
        options.Converters.Add(new KeyJsonConverter());
        options.Converters.Add(new PropertyPathJsonConverter());
        options.Converters.Add(new PropertyPathChildrenDefinitionDictionaryJsonConverter());
        options.Converters.Add(new PropertyExpressionDictionaryConverter());
        options.Converters.Add(new FromDefinitionsConverter());
        options.Converters.Add(new JoinDefinitionsConverter());
        options.Converters.Add(new EventSequenceNumberTokenJsonConverter());

        services.AddSerializer(serializerBuilder => serializerBuilder.AddJsonSerializer(
            _ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Aksio") ?? false),
            options));
    }
}

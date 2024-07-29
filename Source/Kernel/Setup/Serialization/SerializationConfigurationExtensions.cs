// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization;

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
        options.Converters.Add(new KeyJsonConverter());
        options.Converters.Add(new PropertyPathJsonConverter());
        options.Converters.Add(new PropertyPathChildrenDefinitionDictionaryJsonConverter());
        options.Converters.Add(new PropertyExpressionDictionaryConverter());
        options.Converters.Add(new FromDefinitionsConverter());
        options.Converters.Add(new JoinDefinitionsConverter());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriptionJsonConverter, ObserverSubscription>());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriberContextJsonConverter, ObserverSubscriberContext>());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<JobStateJsonConverter, JobState>());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<JobStepStateJsonConverter, JobStepState>());

        services.AddSerializer(serializerBuilder => serializerBuilder.AddJsonSerializer(
            _ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Cratis") ?? false),
            options));
    }
}

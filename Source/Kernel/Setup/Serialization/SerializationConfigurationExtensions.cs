// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Applications.Orleans.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;

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

    /// <summary>
    /// Adds the serializer for appended events.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddAppendedEventSerializer(this IServiceCollection services)
    {
        services.AddSerializer(builder =>
        {
            builder.Services
                .AddCompleteSerializer<AppendedEventSerializer>()
                .AddCompleteSerializer<OneOfSerializer>();
        });
        return services;
    }

    /// <summary>
    /// Add a complete serializer, convenience method when a serializer implements all the interfaces.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <typeparam name="TSerializer">Type of serializer.</typeparam>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCompleteSerializer<TSerializer>(this IServiceCollection services)
        where TSerializer : class, IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
    {
        services.AddSingleton<TSerializer>();
        services.AddSingleton<IGeneralizedCodec, TSerializer>();
        services.AddSingleton<IGeneralizedCopier, TSerializer>();
        services.AddSingleton<ITypeFilter, TSerializer>();

        return services;
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
        options.Converters.Add(new RemovedWithDefinitionsConverter());
        options.Converters.Add(new RemovedWithJoinDefinitionsConverter());
        options.Converters.Add(new JobStateConverter());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriptionJsonConverter, ObserverSubscription>());
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriberContextJsonConverter, ObserverSubscriberContext>());
        services.AddConceptSerializer();
        services.AddAppendedEventSerializer();
        services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
            _ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Cratis") ?? false),
            options));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Extension methods for configuring serialization.
/// </summary>
public static class SerializationConfigurationExtensions
{
    static readonly IEnumerable<JsonConverter> _converters = [
        new EnumConverterFactory(),
        new EnumerableConceptAsJsonConverterFactory(),
        new ConceptAsJsonConverterFactory(),
        new DateOnlyJsonConverter(),
        new TimeOnlyJsonConverter(),
        new TypeJsonConverter(),
        new UriJsonConverter(),
        new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory(),
        new KeyJsonConverter(),
        new PropertyPathJsonConverter(),
        new PropertyPathChildrenDefinitionDictionaryJsonConverter(),
        new PropertyExpressionDictionaryConverter(),
        new FromDefinitionsConverter(),
        new JoinDefinitionsConverter(),
        new RemovedWithDefinitionsConverter(),
        new RemovedWithJoinDefinitionsConverter(),
        new JobStateConverter(),
        new JsonSchemaConverter(),
        new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriptionJsonConverter, ObserverSubscription>(),
        new TypeWithObjectPropertiesJsonConverterFactory<ObserverSubscriberContextJsonConverter, ObserverSubscriberContext>()
    ];

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
    public static IServiceCollection AddCustomSerializers(this IServiceCollection services)
    {
        services.AddSerializer(builder =>
        {
            builder.Services
                .AddCompleteSerializer<AppendedEventSerializer>()
                .AddCompleteSerializer<OneOfSerializer>()
                .AddCompleteSerializer<ConcurrencyScopesSerializer>()
                .AddCompleteSerializer<ExpandoObjectSerializer>();
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
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        ApplyConverters(options);

        services.AddConceptSerializer();
        services.AddCustomSerializers();
        services.AddSerializer(
            serializerBuilder => serializerBuilder.AddJsonSerializer(
            type =>
            {
                // Check if type inherits from OneOfBase - if so, exclude it from JSON serialization
                var current = type;
                while (current != typeof(object) && current is not null)
                {
                    if (current.IsGenericType && current.GetGenericTypeDefinition().Name.Contains("OneOfBase"))
                    {
                        return false;
                    }
                    current = current.BaseType!;
                }

                return type == typeof(JsonObject) || type == typeof(JsonSchema) || (type.Namespace?.StartsWith("Cratis") ?? false);
            },
            options));
    }

    static void ApplyConverters(JsonSerializerOptions options)
    {
        foreach (var converter in _converters)
        {
            options.Converters.Add(converter);
        }
    }
}

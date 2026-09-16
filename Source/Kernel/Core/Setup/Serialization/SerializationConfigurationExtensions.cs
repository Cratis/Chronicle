// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Cratis.Orleans;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Extension methods for configuring serialization.
/// </summary>
public static class SerializationConfigurationExtensions
{
    static readonly IEnumerable<JsonConverter> _converters = [
        new ComplexKeyDictionaryJsonConverterFactory(),
        new EnumConverterFactory(),
        new ConceptDictionaryJsonConverterFactory(),
        new EnumerableConceptAsJsonConverterFactory(),
        new ConceptAsJsonConverterFactory(),
        new DateOnlyJsonConverter(),
        new TimeOnlyJsonConverter(),
        new TypeJsonConverter(),
        new UriJsonConverter(),
        new PointJsonConverter(),
        new LineStringJsonConverter(),
        new PolygonJsonConverter(),
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
        services.AddSingleton<IEventTypeSchemaCache, EventTypeSchemaCache>();
        services.AddSerializer(builder =>
        {
            builder.Services
                .AddCompleteSerializer<AppendedEventSerializer>()
                .AddCompleteSerializer<ConcurrencyScopesSerializer>();
        });
        return services;
    }

    static void Configure(this IServiceCollection services)
    {
        // Pre-warm the global JsonSerializerOptions on this single configuration thread. Its lazy
        // initializer publishes the options instance before it has finished adding the derived-type
        // converter, so two threads racing the first access can freeze it mid-configuration — which
        // surfaces under clustering as "JsonSerializerOptions instance is read-only" when converters
        // such as TypeWithObjectPropertiesJsonConverter serialize across a silo boundary. Touching it
        // here, before any grain runs, makes the first (and only) initialization single-threaded.
        _ = Globals.JsonSerializerOptions;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        ApplyConverters(options);
        services.AddSingleton(options);
        services.AddCratisOrleansSerializers();
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
                    current = current.BaseType;
                }

                // OneOf marker types (e.g. OneOf.Types.None, used as job acknowledgements) have no
                // generated Orleans codec. They must be serializable for failed-partition recovery jobs
                // to start across silo boundaries, so route them through the JSON serializer.
                return type == typeof(JsonObject)
                    || type == typeof(JsonSchema)
                    || type.Namespace == "OneOf.Types"
                    || (type.Namespace?.StartsWith("Cratis") ?? false);
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

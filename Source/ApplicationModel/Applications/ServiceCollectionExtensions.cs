// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Json;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Serialization;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="ServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static JsonSerializerOptions? JsonSerializerOptions;

    internal static void ConfigureJsonSerializerOptions(IDerivedTypes derivedTypes)
    {
        JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ConceptAsJsonConverterFactory(),
                new DateOnlyJsonConverter(),
                new TimeOnlyJsonConverter(),
                new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory(),
                new DerivedTypeJsonConverterFactory(derivedTypes)
            }
        };
    }

    /// <summary>
    /// Add all controllers from all project referenced assemblies.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> for JSON serialization purposes.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddControllersFromProjectReferencedAssembles(this IServiceCollection services, ITypes types, IDerivedTypes derivedTypes)
    {
        if (JsonSerializerOptions is null)
        {
            ConfigureJsonSerializerOptions(derivedTypes);
        }

        var controllerBuilder = services
            .AddControllers(_ => _
                .AddRules()
                .AddCQRS())
            .AddJsonOptions(_ =>
            {
                _.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                foreach (var converter in JsonSerializerOptions!.Converters)
                {
                    _.JsonSerializerOptions.Converters.Add(converter);
                }
            });

        services.AddSingleton(JsonSerializerOptions!);

        foreach (var controllerAssembly in types.ProjectReferencedAssemblies.Where(_ => _.DefinedTypes.Any(type => type.Implements(typeof(Controller)))))
        {
            controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
        }

        return services;
    }
}

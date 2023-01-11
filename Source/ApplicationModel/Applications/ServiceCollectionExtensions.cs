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
    /// <summary>
    /// Add all controllers from all project referenced assemblies.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> for JSON serialization purposes.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddControllersFromProjectReferencedAssembles(this IServiceCollection services, ITypes types, IDerivedTypes derivedTypes)
    {
        Globals.Configure(derivedTypes);

        var controllerBuilder = services
            .AddControllers(_ => _
                .AddValidation(types)
                .AddRules()
                .AddCQRS())
            .AddJsonOptions(_ =>
            {
                _.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                foreach (var converter in Globals.JsonSerializerOptions!.Converters)
                {
                    _.JsonSerializerOptions.Converters.Add(converter);
                }
            });

        services.AddSingleton(Globals.JsonSerializerOptions!);

        foreach (var controllerAssembly in types.ProjectReferencedAssemblies.Where(_ => _.DefinedTypes.Any(type => type.Implements(typeof(Controller)))))
        {
            controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
        }

        return services;
    }
}

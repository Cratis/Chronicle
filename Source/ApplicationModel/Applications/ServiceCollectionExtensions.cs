// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Applications.ModelBinding;
using Aksio.Cratis.Json;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Serialization;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

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
            .AddControllers(options =>
            {
                var bodyModelBinderProvider = options.ModelBinderProviders.First(_ => _ is BodyModelBinderProvider) as BodyModelBinderProvider;
                var complexObjectModelBinderProvider = options.ModelBinderProviders.First(_ => _ is ComplexObjectModelBinderProvider) as ComplexObjectModelBinderProvider;
                options.ModelBinderProviders.Insert(0, new FromRequestModelBinderProvider(bodyModelBinderProvider!, complexObjectModelBinderProvider!));
                options
                    .AddValidation(types)
                    .AddRules()
                    .AddCQRS();
            })
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

        // TODO: Fix this, we're adding the ClientObservers controller assembly from the .NET Client. Should be fixed when we fix #682
        controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(ClientObservers).Assembly));

        return services;
    }
}

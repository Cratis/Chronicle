// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts.SystemJson;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection
{
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
        /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
        public static IServiceCollection AddControllersFromProjectReferencedAssembles(this IServiceCollection services, ITypes types)
        {
            var builder = services.AddControllers(_ => _.AddCQRS());
            foreach (var assembly in types.ProjectReferencedAssemblies.Where(_ => _.DefinedTypes.Any(type => type.Implements(typeof(Controller)))))
            {
                builder.AddJsonOptions(_ => _.JsonSerializerOptions.Converters.Add(new ConceptAsJsonConverterFactory()))
                       .PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            return services;
        }
    }
}

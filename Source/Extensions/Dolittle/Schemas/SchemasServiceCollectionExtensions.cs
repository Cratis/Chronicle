// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using Cratis.Events.Schemas;
using Cratis.Extensions.Dolittle.Schemas;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class SchemasServiceCollectionExtensions
    {
        /// <summary>
        /// Configure use of Dolittle implementations of <see cref="SDK::Cratis.Events.Schemas.ISchemas"/>.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddDolittleSchemas(this IServiceCollection services)
        {
            services.AddSingleton<SDK::Cratis.Events.Schemas.ISchemas, Schemas>();
            services.AddSingleton(new SchemaStoreConfiguration("localhost", 27017));
            return services;
        }
    }
}

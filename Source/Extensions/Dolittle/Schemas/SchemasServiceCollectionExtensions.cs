// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Schemas;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class SchemasServiceCollectionExtensions
    {
        /// <summary>
        /// Configure use of a SchemaStore built on top of Dolittle and how they define events in C#.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <param name="mongoDBHost">The MongoDB hostname.</param>
        /// <param name="mongoDBPort">The MongoDB port.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddDolittleSchemaStore(this IServiceCollection services, string mongoDBHost, int mongoDBPort)
        {
            services.AddSingleton(new SchemaStoreConfiguration(mongoDBHost, mongoDBPort));
            return services;
        }
    }
}

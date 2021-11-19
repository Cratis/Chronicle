// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using Cratis.Events.Projections;
using Cratis.Events.Projections.Changes;
using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.MongoDB;
using Cratis.Extensions.Dolittle.Projections;
using Cratis.Extensions.MongoDB;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ProjectionsServiceCollectionExtensions
    {
        /// <summary>
        /// Configure use of Dolittle implementations of <see cref="SDK::Cratis.Events.Projections.IProjections"/>.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddDolittleProjections(this IServiceCollection services)
        {
            services.AddSingleton<SDK::Cratis.Events.Projections.IProjections, Cratis.Extensions.Dolittle.Projections.Projections>();
            services.AddSingleton<IProjectionPositions, MongoDBProjectionPositions>();
            services.AddSingleton<IChangesetStorage, MongoDBChangesetStorage>();
            services.AddSingleton<IProjectionDefinitionsStorage, MongoDBProjectionDefinitionsStorage>();
            services.AddSingleton(new ProjectionsReady());
            return services;
        }
    }
}

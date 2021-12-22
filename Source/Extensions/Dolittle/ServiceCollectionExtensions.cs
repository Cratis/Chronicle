
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using Cratis.Compliance;
using Cratis.Compliance.InMemory;
using Cratis.Compliance.MongoDB;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure use of Dolittle implementations of <see cref="SDK::Cratis.Events.IEventTypes"/>.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
        /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
        public static IServiceCollection AddDolittleEventTypes(this IServiceCollection services)
        {
            services.AddSingleton<SDK::Cratis.Events.IEventTypes, Cratis.Extensions.Dolittle.EventTypes>();
            services.AddSingleton<IEncryptionKeyStore>((sp) =>
                new CompositeEncryptionKeyStore(
                    sp.GetService<InMemoryEncryptionKeyStore>()!,
                    sp.GetService<MongoDBEncryptionKeyStore>()!
                ));

            return services;
        }
    }
}

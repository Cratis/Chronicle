// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis;
using Cratis.Hosting;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extensions for using Cratis in an application.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IClientBuilder"/> for a non-microservice oriented scenario.
        /// </summary>
        /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
        /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
        /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
        public static IHostBuilder UseCratis(this IHostBuilder hostBuilder, Action<IClientBuilder>? configureDelegate = null)
        {
            return hostBuilder.UseCratis(Guid.Empty, configureDelegate);
        }

        /// <summary>
        /// Configures the <see cref="IClientBuilder"/> for a microservice oriented scenario.
        /// </summary>
        /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
        /// <param name="microserviceId">The unique <see cref="MicroserviceId"/> for the microservice.</param>
        /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
        /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
        public static IHostBuilder UseCratis(this IHostBuilder hostBuilder, MicroserviceId microserviceId, Action<IClientBuilder>? configureDelegate = null)
        {
            var clientBuilder = ClientBuilder.ForMicroservice(microserviceId);
            configureDelegate?.Invoke(clientBuilder);
            hostBuilder.ConfigureServices((context, services) => clientBuilder.Build(context, services));
            return hostBuilder;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis;
using Cratis.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for using Cratis with a <see cref="WebApplicationBuilder"/>.
    /// </summary>
    public static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IClientBuilder"/> for a non-microservice oriented scenario.
        /// </summary>
        /// <param name="webApplicationBuilder"><see cref="WebApplicationBuilder"/> to build on.</param>
        /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
        /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
        public static WebApplicationBuilder UseCratis(this WebApplicationBuilder webApplicationBuilder, Action<IClientBuilder>? configureDelegate = null)
        {
            return webApplicationBuilder.UseCratis(Guid.Empty, configureDelegate);
        }

        /// <summary>
        /// Configures the <see cref="IClientBuilder"/> for a non-microservice oriented scenario.
        /// </summary>
        /// <param name="webApplicationBuilder"><see cref="WebApplicationBuilder"/> to build on.</param>
        /// <param name="microserviceId">The unique <see cref="MicroserviceId"/> for the microservice.</param>
        /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
        /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
        public static WebApplicationBuilder UseCratis(this WebApplicationBuilder webApplicationBuilder, MicroserviceId microserviceId, Action<IClientBuilder>? configureDelegate = null)
        {
            webApplicationBuilder.Host.UseCratis(microserviceId, configureDelegate);
            return webApplicationBuilder;
        }

        /// <summary>
        /// Use Cratis workbench.
        /// </summary>
        /// <param name="webApplicationBuilder"><see cref="WebApplicationBuilder"/> to build on.</param>
        /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
        public static WebApplicationBuilder UseCratisWorkbench(this WebApplicationBuilder webApplicationBuilder)
        {
            webApplicationBuilder.Services.UseCratisWorkbench();
            return webApplicationBuilder;
        }
    }
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Hosting;
using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides extension methods for <see cref="IHostBuilder"/>.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Use Aksio defaults with the <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
        /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
        /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
        public static IHostBuilder UseAksio(this IHostBuilder builder, Action<IClientBuilder>? configureDelegate = default)
        {
            var types = new Types("Aksio");
            types.RegisterTypeConvertersForConcepts();

            builder
                .UseMongoDB(types)
                .UseCratis(types, configureDelegate)
                .ConfigureServices(_ =>
                {
                    _
                    .AddSingleton<ITypes>(types)
                    .AddSingleton<ProviderFor<IServiceProvider>>(() => Internals.ServiceProvider!)
                    .AddConfigurationObjects(types)
                    .AddControllersFromProjectReferencedAssembles(types)
                    .AddSwaggerGen()
                    .AddEndpointsApiExplorer()

                    // Temporarily adding this, due to a bug in .NET 6 (https://www.ingebrigtsen.info/2021/09/29/autofac-asp-net-core-6-hot-reload-debug-crash/):
                    .AddRazorPages();

                    _.AddMvc();
                })
                .UseDefaultLogging()
                .UseDefaultDependencyInversion(types);

            return builder;
        }
}
}

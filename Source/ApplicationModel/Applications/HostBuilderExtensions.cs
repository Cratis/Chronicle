// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio;
using Aksio.Cratis.DependencyInversion;
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
        /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
        public static IHostBuilder UseAksio(this IHostBuilder builder)
        {
            var types = new Types("Aksio");
            builder
                .UseCratis(types)
                .ConfigureServices(_ => _
                    .AddSingleton<ITypes>(types)
                    .AddSingleton<ProviderFor<IServiceProvider>>(() => Internals.ServiceProvider!)
                    .AddControllersFromProjectReferencedAssembles(types)
                    .AddSwaggerGen()

                    // Temporarily adding this, due to a bug in .NET 6 (https://www.ingebrigtsen.info/2021/09/29/autofac-asp-net-core-6-hot-reload-debug-crash/):
                    .AddRazorPages())
                .UseDefaultLogging()
                .UseDefaultDependencyInversion(types);

            return builder;
        }
    }
}

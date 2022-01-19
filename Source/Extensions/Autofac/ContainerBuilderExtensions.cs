// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Extensions.Autofac;
using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Autofac
{
    /// <summary>
    /// Represents extension methods for the Autofac <see cref="ContainerBuilder"/>.
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        internal static ITypes? Types;
        internal static IContainer? Container;

        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> to resolve to.
        /// </summary>
        public static IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Register default Cratis conventions and registrations into the Autofac container.
        /// </summary>
        /// <param name="containerBuilder"><see cref="ContainerBuilder"/> to register into.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="services">Optional services, if configured - will prevent double registrations.</param>
        /// <returns><see cref="ContainerBuilder"/> for build continuation.</returns>
        public static ContainerBuilder RegisterDefaults(this ContainerBuilder containerBuilder, ITypes types, IServiceCollection? services = default)
        {
            Types = types;
            services ??= new ServiceCollection();
            containerBuilder.RegisterInstance(types).As<ITypes>();
            foreach (var moduleType in types.FindMultiple<Module>().Where(_ => _ != typeof(DefaultConventionModule)))
            {
                containerBuilder.RegisterModule((Module)Activator.CreateInstance(moduleType)!);
            }
            containerBuilder.RegisterModule(new DefaultConventionModule(services));

            containerBuilder.RegisterSource<SelfBindingRegistrationSource>();
            containerBuilder.Register(_ => Container!).As<IContainer>().SingleInstance();
            containerBuilder.RegisterBuildCallback(_ => Container = (IContainer)_!);

            containerBuilder.RegisterSource(new SingletonPerTenantRegistrationSource(Types));
            containerBuilder.RegisterSource(new ProviderForRegistrationSource());

            return containerBuilder;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;

namespace Cratis.Server
{
    public class ClientServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            var builder = new ContainerBuilder();
            builder.Populate(services);

            foreach (var registration in Startup.AutofacContainer!.ComponentRegistry.Registrations.Where(_ => !_.Services.Any(s => s.Description.Contains("Orleans", StringComparison.InvariantCulture))))
            {
                builder.ComponentRegistryBuilder.Register(registration);
            }

            return builder;
        }

        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            var container = containerBuilder.Build(ContainerBuildOptions.None);
            return new AutofacServiceProvider(container);
        }
    }
}

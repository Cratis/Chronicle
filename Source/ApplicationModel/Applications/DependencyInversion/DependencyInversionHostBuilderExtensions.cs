// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for working with dependency inversion.
/// </summary>
public static class DependencyInversionHostBuilderExtensions
{
    /// <summary>
    /// Use the default dependency inversion setup.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to use with.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <returns><see cref="IHostBuilder"/> for continuation.</returns>
    public static IHostBuilder UseDefaultDependencyInversion(this IHostBuilder builder, ITypes types)
    {
        builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        IServiceCollection? services = default;
        builder.ConfigureServices(_ => services = _);
        builder.ConfigureContainer<ContainerBuilder>(containerBuilder => containerBuilder.RegisterDefaults(types, services));
        return builder;
    }
}

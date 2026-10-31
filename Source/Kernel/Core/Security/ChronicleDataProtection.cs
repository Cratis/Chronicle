// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Composes the persistent Data Protection key ring shared by Chronicle hosts.
/// </summary>
public static class ChronicleDataProtection
{
    const string FrameworkWarmup = "Microsoft.AspNetCore.DataProtection.Internal.DataProtectionHostedService";

    /// <summary>
    /// Adds the shared key ring, independently of whether authentication is enabled.
    /// </summary>
    /// <param name="services">The services to configure.</param>
    /// <returns>The Data Protection builder for host-specific certificate configuration.</returns>
    public static IDataProtectionBuilder AddChronicleDataProtection(this IServiceCollection services)
    {
        var builder = services.AddDataProtection().SetApplicationName("Chronicle");
        if (!services.Any(_ => _.ServiceType == typeof(SharedKeyRing)))
        {
            services.AddSingleton<SharedKeyRing>();
            services.TryAddSingleton<GrainBasedXmlRepository>();
            services.TryAddSingleton<IXmlRepository>(provider => provider.GetRequiredService<GrainBasedXmlRepository>());
            services.AddOptions<KeyManagementOptions>()
                .Configure<IXmlRepository>((options, repository) => options.XmlRepository = repository);
        }

        services.DeferChronicleDataProtectionWarmup();
        return builder;
    }

    /// <summary>
    /// Defers only the framework warmup. Call after additional framework Data Protection registrations.
    /// </summary>
    /// <param name="services">The services to configure.</param>
    /// <returns>The services for chaining.</returns>
    /// <exception cref="InvalidOperationException">The warmup has an unowned registration.</exception>
    public static IServiceCollection DeferChronicleDataProtectionWarmup(this IServiceCollection services)
    {
        var registered = services.Any(_ => _.ServiceType == typeof(DeferredWarmup));
        if (!registered && services.Any(_ => _.ServiceType == typeof(DataProtectionKeyRingWarmup)))
        {
            throw new InvalidOperationException("DataProtectionKeyRingWarmup must be registered by Chronicle Data Protection.");
        }

        foreach (var original in services.Where(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType?.FullName == FrameworkWarmup).ToArray())
        {
            var index = services.IndexOf(original);
            services.RemoveAt(index);
            if (registered) continue;
            services.AddSingleton<DeferredWarmup>();
            registered = true;
            services.AddSingleton(provider => new DataProtectionKeyRingWarmup((IHostedService)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!)));
            services.Insert(index, ServiceDescriptor.Singleton<IHostedService>(provider => provider.GetRequiredService<DataProtectionKeyRingWarmup>()));
        }

        return services;
    }

    [IgnoreConvention]
    sealed class DeferredWarmup;

    sealed class SharedKeyRing;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Orleans.Hosting;

/// <summary>
/// Activates single-writer protection only for hosts that explicitly select it.
/// </summary>
public static class SingleWriterLeaseSiloBuilderExtensions
{
    /// <summary>
    /// Acquires a storage lease before runtime initialization and exposes readiness.
    /// </summary>
    /// <param name="builder">The silo builder.</param>
    /// <returns>The builder for further configuration.</returns>
    public static ISiloBuilder AddSingleWriterLease(this ISiloBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<ISingleWriterLeaseFailFast, SingleWriterLeaseFailFast>();
            services.AddSingleton<SingleWriterLeaseKeeper>();
            services.AddSingleton<ISingleWriterLease>(provider => provider.GetRequiredService<SingleWriterLeaseKeeper>());
            services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(provider => provider.GetRequiredService<SingleWriterLeaseKeeper>());
            services.AddHealthChecks().AddCheck<SingleWriterLeaseHealthCheck>("single-writer-lease", tags: ["ready"]);
        });
        return builder;
    }
}

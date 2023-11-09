// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Workers;

#pragma warning disable SA1600
namespace Aksio.Cratis.Kernel.Server;

/// <summary>
/// Extension methods for configuring Cpu bound workers.
/// </summary>
public static class CpuBoundWorkersExtensions
{
    /// <summary>
    /// Configure the Cpu bound workers.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to configure.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder ConfigureCpuBoundWorkers(this ISiloBuilder builder)
    {
        var maxLevelOfParallelism = Environment.ProcessorCount - 2;
        if (maxLevelOfParallelism < 0)
        {
            maxLevelOfParallelism = 1;
        }
        builder.ConfigureServices((services) => services.AddSingleton((IServiceProvider _) => new LimitedConcurrencyLevelTaskScheduler(maxLevelOfParallelism)));

        return builder;
    }
}

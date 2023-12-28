// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Workers;
using Serilog;

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
    public static IHostBuilder ConfigureCpuBoundWorkers(this IHostBuilder builder)
    {
        Log.Logger.Information("Configuring Cpu bound workers");

        var maxLevelOfParallelism = Environment.ProcessorCount - 2;
        if (maxLevelOfParallelism <= 0)
        {
            maxLevelOfParallelism = 1;
        }

        Log.Logger.Information("Max level of parallelism for Cpu bound workers: {MaxLevelOfParallelism} - number of processor count {ProcessorCount}", maxLevelOfParallelism, Environment.ProcessorCount);

        builder.ConfigureServices((services) => services.AddSingleton(new LimitedConcurrencyLevelTaskScheduler(maxLevelOfParallelism)));

        return builder;
    }
}

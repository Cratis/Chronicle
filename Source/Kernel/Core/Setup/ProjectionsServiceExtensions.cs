// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Telemetry;

#pragma warning disable SA1600
namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for projections initialization.
/// </summary>
public static class ProjectionsServiceExtensions
{
    /// <summary>
    /// Add projections.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddProjectionsService(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<ProjectionsService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IProjectionsServiceClient, ProjectionsServiceClient>());

        // Based on https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/#custom-resilience-pipeline-and-dynamic-reloads
        siloBuilder.Services.Configure<ResilientProjectionsServiceOptions>(
            ProjectionsServiceClient.ResiliencePipelineKey,
            siloBuilder.Configuration.GetSection(ConfigurationPath.Combine("Chronicle", "ResilientProjectionsService")));
        siloBuilder.Services.AddResiliencePipeline(ProjectionsServiceClient.ResiliencePipelineKey, static (builder, context) =>
        {
            context.EnableReloads<ResilientProjectionsServiceOptions>();
            var options = context.GetOptions<ResilientProjectionsServiceOptions>();
            builder.AddRetry(options.Retry);
            builder.AddTimeout(options.Timeout);

            var telemetryOptions = new TelemetryOptions(context.GetOptions<TelemetryOptions>())
            {
                SeverityProvider = ev =>
                {
                    if (options.ResilienceEventSeverities.TryGetValue(ev.Event.EventName, out var severity))
                    {
                        return severity;
                    }

                    return ev.Event.Severity;
                }
            };
            builder.ConfigureTelemetry(telemetryOptions);
        });

        return siloBuilder;
    }
}

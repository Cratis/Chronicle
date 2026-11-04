// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.Identifiers;
using Cratis.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Chronicle defaults and instrumentation for the shared telemetry setup.
/// </summary>
public static class TelemetryConfiguration
{
    /// <summary>
    /// Adds the shared telemetry setup with Chronicle instrumentation, honoring the configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> holding the Chronicle options and the standard OTEL keys.</param>
    /// <param name="serviceName">Optional fallback service name; standard OTEL settings still take precedence.</param>
    /// <returns>The <see cref="OpenTelemetryBuilder"/> for continuation.</returns>
    /// <remarks>
    /// The identifier span processor is registered before the shared setup registers its exporters, because processors run
    /// in registration order and the grain ids must be removed before an exporter reads the span.
    /// </remarks>
    public static OpenTelemetryBuilder AddChronicleOpenTelemetry(this IServiceCollection services, IConfiguration configuration, string? serviceName = null)
    {
        var builder = services.AddOpenTelemetry();
        builder.WithTracing(tracing => tracing.AddProcessor(_ => new IdentifierSpanProcessor()));
        return builder.WithCratis(configuration, options =>
        {
            Configure(options, configuration);
            if (serviceName is not null)
            {
                options.ServiceName = serviceName;
            }
        });
    }

    /// <summary>
    /// Configures Chronicle instrumentation with default <see cref="ChronicleOptions"/>, without overriding standard OTEL resource or export settings.
    /// </summary>
    /// <param name="options">The shared telemetry options.</param>
    public static void Configure(CratisOpenTelemetryOptions options) => Configure(options, new ChronicleOptions());

    /// <summary>
    /// Configures Chronicle instrumentation, honoring the health check endpoint and telemetry options from the configuration, without overriding standard OTEL resource or export settings.
    /// </summary>
    /// <param name="options">The shared telemetry options.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> holding the Chronicle options and the standard OTEL keys.</param>
    public static void Configure(CratisOpenTelemetryOptions options, IConfiguration configuration)
    {
        MaybeSetHttp2Unencrypted(configuration);
        Configure(options, configuration.GetSection(ChronicleOptions.SectionPath).Get<ChronicleOptions>() ?? new());
    }

    static void Configure(CratisOpenTelemetryOptions options, ChronicleOptions chronicle)
    {
        options.ServiceName = "Chronicle";
        options.IncludeExceptionDetails = true;
        options.ConfigureTracing = tracing => ConfigureTracing(tracing, chronicle);
        options.ConfigureMetrics = ConfigureMetrics;
    }

    static void ConfigureTracing(TracerProviderBuilder tracing, ChronicleOptions chronicle)
    {
        // The shared setup already adds HTTP client, ASP.NET Core and runtime instrumentation and the OTLP exporters.
        // Only the ASP.NET Core filter is replaced, through its options, so that instrumentation is not added twice.
        var healthCheckEndpoint = chronicle.HealthCheckEndpoint;
        tracing
            .ConfigureServices(services => services.Configure<AspNetCoreTraceInstrumentationOptions>(instrumentation =>
                instrumentation.Filter = context => ServerRequestTraceFilter.ShouldTrace(healthCheckEndpoint, context.Request.Path.Value)))
            .AddChronicleInstrumentation();

        if (chronicle.Telemetry.GrpcClientTracing)
        {
            tracing.AddGrpcClientInstrumentation();
        }
    }

    static void MaybeSetHttp2Unencrypted(IConfiguration configuration)
    {
        var endpoints = new[]
        {
            configuration["OTEL_EXPORTER_OTLP_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP:Endpoint"]
        }.Where(_ => !string.IsNullOrWhiteSpace(_)).ToArray();

        if (endpoints.Length == 0 || endpoints.Any(_ => Uri.TryCreate(_, UriKind.Absolute, out var uri) && uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)))
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }

    static void ConfigureMetrics(MeterProviderBuilder metrics)
    {
        // Reduce pre-allocated MetricPoint arrays from the default 2000 to 500.
        // The default causes ~25 MB of committed memory at startup for all metric instruments.
        // The limit applies per instrument. Chronicle's observer instruments carry one series per observer
        // in each event store, namespace and event sequence, and never one per partition or event source.
        // The failure instruments only get a series once an observer fails, so the series they hold follow
        // the failing observers, while the successful observations instrument holds one per active observer.
        // Exceeding 500 therefore takes more than 500 active observer instances (observers x namespaces x
        // event stores). Raise the limit before that is reached: the SDK folds the excess into an overflow
        // series that has none of the tags, and those measurements can no longer be attributed to an observer.
        // Do not add a second view for a single instrument: every view that matches an instrument produces
        // its own stream for it, so the instrument would be exported twice. Change this one view instead.
        metrics
            .AddView("*", new MetricStreamConfiguration { CardinalityLimit = 500 })
            .AddChronicleInstrumentation();
    }
}

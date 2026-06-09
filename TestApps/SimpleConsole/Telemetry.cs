// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TestApp;

/// <summary>
/// Bootstraps OpenTelemetry tracing and metrics for the console sample.
/// </summary>
/// <remarks>
/// Only activates when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set in the environment.
/// Override the service name via <c>SERVICE_NAME</c> and the version via <c>SERVICE_VERSION</c>.
/// This class MUST be invoked before the Chronicle client is created so that all
/// client-side spans are captured by the active TracerProvider.
/// </remarks>
public static class Telemetry
{
    /// <summary>
    /// Builds and starts the OpenTelemetry SDK providers.
    /// </summary>
    /// <returns>
    /// A disposable <see cref="TelemetryHandles"/> that flushes and shuts down the providers on
    /// disposal, or a no-op handle when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is not set.
    /// </returns>
    public static TelemetryHandles Build()
    {
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new TelemetryHandles(null, null);
        }

        // OTLP over gRPC requires HTTP/2. For plain http:// OTLP endpoints (typical in local
        // development), the .NET HTTP client must allow HTTP/2 without TLS.
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "chronicle-test-app";
        var serviceVersion = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? "1.0.0";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);

        var tracers = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resource)
            .AddSource(ClientActivity.SourceName)
            .AddHttpClientInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddOtlpExporter()
            .Build();

        var metrics = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter()
            .Build();

        return new TelemetryHandles(tracers, metrics);
    }
}

/// <summary>
/// Holds the active OpenTelemetry provider handles returned by <see cref="Telemetry.Build"/>.
/// </summary>
/// <param name="Tracers">
/// The active <see cref="TracerProvider"/>, or <see langword="null"/> when telemetry is disabled.
/// </param>
/// <param name="Metrics">
/// The active <see cref="MeterProvider"/>, or <see langword="null"/> when telemetry is disabled.
/// </param>
public sealed record TelemetryHandles(TracerProvider? Tracers, MeterProvider? Metrics) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        Tracers?.Dispose();
        Metrics?.Dispose();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration.given;

public class a_telemetry_configuration : Specification
{
    protected ServiceCollection _services;
    protected IConfigurationRoot _configuration;
    protected ServiceProvider _provider;
    protected TracerProvider _traces;
    protected MeterProvider _metrics;
    protected LoggerProvider _logs;
    protected List<OtlpExporterOptions> _exporterOptions;
    Dictionary<string, string?> _environment;

    void Establish()
    {
        string[] keys =
        [
            "OTEL_SERVICE_NAME", "OTEL_RESOURCE_ATTRIBUTES", "OTEL_SDK_DISABLED",
            "OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
            "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT",
            "OTEL_TRACES_EXPORTER", "OTEL_METRICS_EXPORTER", "OTEL_LOGS_EXPORTER"
        ];
        _environment = keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
        foreach (var key in keys) Environment.SetEnvironmentVariable(key, null);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _services = new ServiceCollection();
        _services.AddSingleton<IConfiguration>(_configuration);
        _exporterOptions = [];
        _services.Configure<OtlpExporterOptions>(options =>
        {
            // The shared setup creates exporter options once for each enabled signal.
            _exporterOptions.Add(options);
            options.HttpClientFactory = () => new HttpClient(new an_otlp_receiver());
        });
    }

    protected void ResolveProviders()
    {
        _provider = _services.BuildServiceProvider();
        _traces = _provider.GetRequiredService<TracerProvider>();
        _metrics = _provider.GetRequiredService<MeterProvider>();
        _logs = _provider.GetRequiredService<LoggerProvider>();
    }

    void Destroy()
    {
        _provider?.Dispose();
        (_configuration as IDisposable)?.Dispose();
        foreach (var (key, value) in _environment) Environment.SetEnvironmentVariable(key, value);
    }
}

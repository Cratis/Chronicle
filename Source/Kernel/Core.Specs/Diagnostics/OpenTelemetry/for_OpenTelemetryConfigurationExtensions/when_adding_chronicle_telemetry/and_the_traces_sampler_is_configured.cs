// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_telemetry;

[Collection(nameof(given.orleans_activity_source_collection))]
public class and_the_traces_sampler_is_configured : Specification
{
    ServiceProvider _services;
    Activity _span;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_TRACES_SAMPLER"] = "always_off",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1"
        }).Build();
        _services = new ServiceCollection().AddSingleton<IConfiguration>(configuration)
            .AddChronicleTelemetry(configuration).BuildServiceProvider();
        _services.GetRequiredService<TracerProvider>();
    }

    void Because()
    {
        _span = ChronicleActivity.Source.StartActivity("probe");
        _span?.Stop();
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_not_record_the_span() => (_span?.Recorded != true).ShouldBeTrue();
}

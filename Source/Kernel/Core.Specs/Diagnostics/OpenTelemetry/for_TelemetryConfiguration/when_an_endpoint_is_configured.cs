// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_an_endpoint_is_configured : given.a_telemetry_configuration
{
    void Establish() => Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "https://collector.example:4318");

    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, TelemetryConfiguration.Configure);
        ResolveProviders();
    }

    [Fact] void should_create_an_exporter_for_each_signal() => _exporterOptions.Count.ShouldEqual(3);
    [Fact] void should_use_the_configured_endpoint() => _exporterOptions.ShouldEachConformTo(_ => _.Endpoint.Host == "collector.example");
}

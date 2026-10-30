// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_a_signal_endpoint_is_configured : given.a_telemetry_configuration
{
    void Establish() => _configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = "https://traces.example:4318/v1/traces";

    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, TelemetryConfiguration.Configure);
        ResolveProviders();
    }

    [Fact] void should_create_only_one_exporter() => _exporterOptions.Count.ShouldEqual(1);
    [Fact] void should_use_the_signal_endpoint() => _exporterOptions.Single().Endpoint.ShouldEqual(new Uri("https://traces.example:4318/v1/traces"));
}

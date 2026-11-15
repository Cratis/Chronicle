// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions;

public class when_an_endpoint_is_configured : Specification
{
    bool _general;
    bool _section;
    bool _environment;
    bool _otherSignal;
    bool _signalSpecific;

    void Because()
    {
        _general = HasEndpoint("OTEL_EXPORTER_OTLP_ENDPOINT", "TRACES");
        _section = HasEndpoint("OTEL_EXPORTER_OTLP:Endpoint", "METRICS");
        _signalSpecific = HasEndpoint("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "METRICS");
        _otherSignal = HasEndpoint("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "TRACES");
        _environment = OpenTelemetryConfigurationExtensions.HasOtlpEndpoint(
            new ConfigurationBuilder().Build(),
            "LOGS",
            name => name == "OTEL_EXPORTER_OTLP_ENDPOINT" ? "http://collector:4317" : null);
    }

    static bool HasEndpoint(string key, string signal) =>
        OpenTelemetryConfigurationExtensions.HasOtlpEndpoint(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { key, "http://collector:4317" } }).Build(),
            signal,
            _ => null);

    [Fact] void should_export_for_the_general_endpoint() => _general.ShouldBeTrue();
    [Fact] void should_export_for_the_section_endpoint() => _section.ShouldBeTrue();
    [Fact] void should_export_for_the_signal_endpoint() => _signalSpecific.ShouldBeTrue();
    [Fact] void should_not_export_another_signal() => _otherSignal.ShouldBeFalse();
    [Fact] void should_export_for_an_environment_endpoint() => _environment.ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions;

public class when_no_endpoint_is_configured : Specification
{
    bool _traces;
    bool _metrics;
    bool _logs;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "OTEL_EXPORTER_OTLP_ENDPOINT", "  " } }).Build();
        _traces = OpenTelemetryConfigurationExtensions.HasOtlpEndpoint(configuration, "TRACES", _ => null);
        _metrics = OpenTelemetryConfigurationExtensions.HasOtlpEndpoint(configuration, "METRICS", _ => null);
        _logs = OpenTelemetryConfigurationExtensions.HasOtlpEndpoint(configuration, "LOGS", _ => null);
    }

    [Fact] void should_not_export_traces() => _traces.ShouldBeFalse();
    [Fact] void should_not_export_metrics() => _metrics.ShouldBeFalse();
    [Fact] void should_not_export_logs() => _logs.ShouldBeFalse();
}

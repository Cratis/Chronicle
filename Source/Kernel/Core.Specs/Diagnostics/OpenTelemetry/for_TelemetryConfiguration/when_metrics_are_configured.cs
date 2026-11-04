// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_metrics_are_configured : given.a_telemetry_configuration
{
    readonly List<string> _exported = [];
    Meter _meter;

    void Establish()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, options => TelemetryConfiguration.Configure(options, _configuration));
        _services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddReader(new BaseExportingMetricReader(new RecordingExporter(_exported))));
        ResolveProviders();
        _meter = new Meter("Microsoft.Orleans");
    }

    void Because()
    {
        var counter = _meter.CreateCounter<long>("view_probe");
        counter.Add(1);
        _metrics.ForceFlush();
    }

    [Fact] void should_export_the_instrument_once_because_there_is_a_single_view() => _exported.Count(_ => _ == "view_probe").ShouldEqual(1);

    void Destroy() => _meter.Dispose();

    sealed class RecordingExporter(List<string> exported) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                exported.Add(metric.Name);
            }

            return ExportResult.Success;
        }
    }
}

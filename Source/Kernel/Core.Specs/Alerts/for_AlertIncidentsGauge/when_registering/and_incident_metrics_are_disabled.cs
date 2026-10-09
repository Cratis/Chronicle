// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_registering;

public class and_incident_metrics_are_disabled : Specification
{
    Meter _meter;
    ServiceProvider _services;
    AlertIncidentsGauge _gauge;
    readonly List<Instrument> _instruments = [];
    int _measurements;

    void Establish()
    {
        _meter = new($"test-{Guid.NewGuid():N}");
        var meter = Substitute.For<IMeter<AlertIncidentsGauge>>();
        meter.ActualMeter.Returns(_meter);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{ChronicleOptions.SectionPath}:Alerts:IncidentMetricsEnabled"] = "false"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ChronicleOptions>().BindConfiguration(ChronicleOptions.SectionPath);
        _services = services.BuildServiceProvider();
        _gauge = ActivatorUtilities.CreateInstance<AlertIncidentsGauge>(_services, meter);
    }

    void Because()
    {
        IAlertIncidentsGauge holder = _gauge;
        holder.Activate(this);
        holder.Publish(this, AlertIncidentsGaugeSnapshot.From(DateTimeOffset.UtcNow,
            [(new AlertIncidentGaugeBucket("store", "Default", "observer", "event-log", "partition-failing", Concepts.Alerts.AlertSeverity.Warning), 2)]));
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == _meter)
            {
                _instruments.Add(instrument);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => _measurements++);
        listener.SetMeasurementEventCallback<int>((_, _, _, _) => _measurements++);
        listener.Start();
        listener.RecordObservableInstruments();
    }

    void Destroy()
    {
        _services.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_register_no_incident_instruments() => _instruments.ShouldBeEmpty();
    [Fact] void should_collect_no_measurements() => _measurements.ShouldEqual(0);
    [Fact] void should_produce_no_incident_measurements() => _gauge.Observe().ShouldBeEmpty();
    [Fact] void should_produce_no_availability_measurements() => _gauge.ObserveAvailable().ShouldBeEmpty();
}

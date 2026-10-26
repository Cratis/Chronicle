// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyMetricNameBridge.given;

public class a_bridge_on_a_private_meter : Specification
{
    protected const string Convention = "cratis.chronicle.spec.convention";
    protected const string ConventionHistogram = "cratis.chronicle.spec.duration";
    protected const string ConventionGauge = "cratis.chronicle.spec.level";
    protected const string ConventionTwoLegacy = "cratis.chronicle.spec.two";
    protected const string NotMapped = "cratis.chronicle.spec.not_mapped";

    protected Meter _meter = null!;
    protected Meter _otherMeter = null!;
    protected LegacyMetricNameBridge _bridge = null!;
    protected List<Mirrored> _mirrored = [];
    MeterListener _listener = null!;

    protected record Mirrored(string Name, string? Unit, string? Description, double Value, KeyValuePair<string, object?>[] Tags);

    void Establish()
    {
        var name = $"bridge-specs-{Guid.NewGuid():N}";
        _meter = new Meter(name);
        _otherMeter = new Meter($"{name}-other");

        var map = new Dictionary<string, LegacyMetric[]>
        {
            [Convention] = [new("legacy-counter", null, "A legacy counter")],
            [ConventionHistogram] = [new("legacy-duration", "s", "A legacy duration")],
            [ConventionGauge] = [new("legacy-level", null, "A legacy level")],
            [ConventionTwoLegacy] = [new("legacy-one", null, null), new("legacy-two", null, null)]
        };

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name.StartsWith("legacy-", StringComparison.Ordinal) && instrument.Meter.Name.StartsWith(name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.Start();

        _bridge = new LegacyMetricNameBridge(meter => ReferenceEquals(meter, _meter), map);
    }

    void Destroy()
    {
        _bridge.Dispose();
        _listener.Dispose();
        _meter.Dispose();
        _otherMeter.Dispose();
    }

    void Capture<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        where T : struct
    {
        lock (_mirrored)
        {
            _mirrored.Add(new(instrument.Name, instrument.Unit, instrument.Description, Convert.ToDouble(value), tags.ToArray()));
        }
    }
}

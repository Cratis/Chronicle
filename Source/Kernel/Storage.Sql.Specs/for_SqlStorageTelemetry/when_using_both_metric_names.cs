// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_SqlStorageTelemetry;

public class when_using_both_metric_names : Specification
{
    readonly MeterListener _listener = new();
    readonly List<Instrument> _instruments = [];
    readonly List<(string Name, int Value, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    SqlStorageTelemetry _telemetry;
    string[] _namesBeforeRecording = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (!instrument.Name.Contains(".sql.", StringComparison.Ordinal) && !instrument.Name.StartsWith("chronicle-sql-", StringComparison.Ordinal)) return;
            lock (_instruments)
            {
                _instruments.Add(instrument);
            }

            listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) =>
        {
            if (tags.Length == 1 && Equals(tags[0].Value, "both-metric-names-spec"))
            {
                _measurements.Add((instrument.Name, value, tags.ToArray()));
            }
        });
        _listener.Start();
        _telemetry = new(MetricNames.Both);
        _ = new SqlCompositeKeyShadow(MetricNames.Both);
        _namesBeforeRecording = InstrumentNames();
    }

    /// <summary>Gets the distinct names seen so far; other specs may publish instruments concurrently (static shadow meter), so names are compared rather than counts.</summary>
    /// <returns>Sorted distinct instrument names.</returns>
    string[] InstrumentNames()
    {
        lock (_instruments)
        {
            return _instruments.Select(_ => _.Name).Distinct().Order().ToArray();
        }
    }

    void Because() => _telemetry.RecoveredCollision("both-metric-names-spec", 3);

    [Fact]
    void should_precreate_both_sets() => new[]
    {
        "chronicle-sql-schema-lock-wait",
        "chronicle-sql-live-query-poll-failures",
        "chronicle-sql-upsert-collisions-recovered",
        "chronicle.sql.composite_key.stamp_failures",
        "chronicle.sql.composite_key.collisions",
        "cratis.chronicle.sql.schema_lock.wait_duration",
        "cratis.chronicle.sql.live_query.poll_failures",
        "cratis.chronicle.sql.upsert.recovered_collisions",
        "cratis.chronicle.sql.composite_key.stamp_failures",
        "cratis.chronicle.sql.composite_key.collisions"
    }.Order().Except(InstrumentNames()).ShouldBeEmpty();
    [Fact] void should_not_create_instruments_while_recording() => InstrumentNames().Where(_ => _.Contains("upsert", StringComparison.Ordinal)).ShouldEqual(_namesBeforeRecording.Where(_ => _.Contains("upsert", StringComparison.Ordinal)));
    [Fact] void should_emit_only_the_recorded_collision() => _measurements.Count.ShouldEqual(2);
    [Fact] void should_record_the_original_value_to_both() => _measurements.Select(_ => _.Value).ShouldEqual([3, 3]);
    [Fact] void should_preserve_the_legacy_unit() => _instruments.Exists(_ => _.Name == "chronicle-sql-upsert-collisions-recovered" && _.Unit is null).ShouldBeTrue();
    [Fact] void should_publish_the_convention_unit() => _instruments.Exists(_ => _.Name == "cratis.chronicle.sql.upsert.recovered_collisions" && _.Unit == "{collision}").ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_key() => _measurements.Single(_ => _.Name == "chronicle-sql-upsert-collisions-recovered").Tags.ShouldEqual([new("operation", "both-metric-names-spec")]);
    [Fact] void should_publish_the_convention_key() => _measurements.Single(_ => _.Name == "cratis.chronicle.sql.upsert.recovered_collisions").Tags.ShouldEqual([new("cratis.chronicle.sql.upsert.operation", "both-metric-names-spec")]);
    [Fact] void should_preserve_the_shadow_failure_name() => _instruments.Exists(_ => _.Name == "chronicle.sql.composite_key.stamp_failures").ShouldBeTrue();
    [Fact] void should_publish_the_shadow_failure_convention_name() => _instruments.Exists(_ => _.Name == "cratis.chronicle.sql.composite_key.stamp_failures").ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}

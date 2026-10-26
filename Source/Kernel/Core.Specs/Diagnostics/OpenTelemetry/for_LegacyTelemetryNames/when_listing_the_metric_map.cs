// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_LegacyTelemetryNames;

public class when_listing_the_metric_map : Specification
{
    IEnumerable<string> _conventionMetricNames = [];

    void Establish() => _conventionMetricNames = typeof(WellKnownMetricNames)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(_ => _.IsLiteral && !_.Name.EndsWith("Unit"))
        .Select(_ => (string)_.GetRawConstantValue()!)
        .ToArray();

    [Fact] void should_map_or_explicitly_exclude_every_metric() =>
        _conventionMetricNames.Where(_ => !LegacyTelemetryNames.Metrics.ContainsKey(_) && !LegacyTelemetryNames.MetricsWithoutBridge.Contains(_)).ShouldBeEmpty();

    [Fact] void should_not_map_unknown_metrics() =>
        LegacyTelemetryNames.Metrics.Keys.Where(_ => !_conventionMetricNames.Contains(_)).ShouldBeEmpty();

    [Fact] void should_not_map_what_is_excluded() =>
        LegacyTelemetryNames.MetricsWithoutBridge.Any(LegacyTelemetryNames.Metrics.ContainsKey).ShouldBeFalse();

    [Fact] void should_not_publish_the_same_legacy_name_twice() =>
        LegacyTelemetryNames.Metrics.Values.SelectMany(_ => _).GroupBy(_ => _.Name).Any(_ => _.Count() > 1).ShouldBeFalse();

    [Fact] void should_not_bridge_the_retry_attempts_that_repeat_failures() =>
        LegacyTelemetryNames.Metrics.Values.SelectMany(_ => _).Any(_ => _.Name == LegacyTelemetryNames.PartitionRetryAttempts).ShouldBeFalse();

    [Fact] void should_translate_every_legacy_attribute_key_to_a_distinct_key() =>
        LegacyTelemetryNames.MetricAttributeKeys.Values.Distinct().Count().ShouldEqual(LegacyTelemetryNames.MetricAttributeKeys.Count);
}

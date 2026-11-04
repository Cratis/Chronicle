// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.for_ObserverMetrics.given;
using Cratis.Chronicle.Observation.for_ObserverMetrics.given.Otlp;
using Cratis.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using ProtoBuf;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_exporting_observer_metrics_as_otlp : Specification
{
    readonly Uri _endpoint = new("http://otlp.invalid/v1/metrics");
    readonly Dictionary<string, object> _tags = new()
    {
        ["EventStore"] = "otlp-store",
        ["Namespace"] = "otlp-namespace",
        ["ObserverId"] = $"otlp-observer-{Guid.NewGuid():N}",
        ["EventSequenceId"] = "otlp-sequence"
    };
    CapturingHttpMessageHandler _handler;
    HttpClient _client;
    MeterProvider _provider;
    bool _flushed;
    (HttpMethod Method, Uri? Uri, string? ContentType, byte[] Body)[] _requests;
    (OtlpMetric Metric, OtlpNumberDataPoint Point)[] _successfulObservations;
    (OtlpMetric Metric, OtlpNumberDataPoint Point)[] _partitionFailures;

    void Establish()
    {
        _handler = new();
        _client = new(_handler);
        _provider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(ObserverMetricsRecorder.SharedMeter.Name)
            .AddOtlpExporter(options =>
            {
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                options.Endpoint = _endpoint;
                options.HttpClientFactory = () => _client;
            })
            .Build();
    }

    void Because()
    {
        var observer = new Meter<Observer>(ObserverMetricsRecorder.SharedMeter).BeginObserverScope(
            (string)_tags["ObserverId"],
            new((string)_tags["ObserverId"], (string)_tags["EventStore"], (string)_tags["Namespace"], (string)_tags["EventSequenceId"]));
        observer.SuccessfulObservation();
        observer.PartitionFailed();
        _flushed = _provider.ForceFlush(10_000);

        // Snapshot before teardown: periodic and disposal exports need not have global request/metric counts.
        _requests = _handler.Snapshot();
        var metrics = _requests.SelectMany(request => Decode(request.Body).ResourceMetrics)
            .SelectMany(resource => resource.ScopeMetrics)
            .SelectMany(scope => scope.Metrics)
            .ToArray();
        _successfulObservations = FindPoints(metrics, "chronicle-observer-successful-observations");
        _partitionFailures = FindPoints(metrics, "chronicle-observer-partitions-failed");
    }

    void Destroy()
    {
        _provider?.Dispose();
        _client?.Dispose();
    }

    static OtlpMetricsRequest Decode(byte[] body)
    {
        using var stream = new MemoryStream(body);
        return Serializer.Deserialize<OtlpMetricsRequest>(stream);
    }

    (OtlpMetric Metric, OtlpNumberDataPoint Point)[] FindPoints(IEnumerable<OtlpMetric> metrics, string name) =>
        metrics.Where(metric => metric.Name == name)
            .SelectMany(metric => (metric.Sum?.DataPoints ?? []).Select(point => (Metric: metric, Point: point)))
            .Where(entry => entry.Point.Attributes.Exists(attribute => attribute.Key == "ObserverId" && attribute.Value?.StringValue == (string)_tags["ObserverId"]))
            .ToArray();

    bool HasExpectedTags(OtlpNumberDataPoint point) =>
        point.Attributes.Count == _tags.Count &&
        _tags.All(tag => point.Attributes.Exists(attribute => attribute.Key == tag.Key && attribute.Value?.StringValue == (string)tag.Value));

    [Fact] void should_flush_the_sdk_exporter() => _flushed.ShouldBeTrue();
    [Fact] void should_capture_a_request() => _requests.ShouldNotBeEmpty();
    [Fact] void should_post_to_the_configured_endpoint() => _requests.All(request => request.Method == HttpMethod.Post && request.Uri == _endpoint).ShouldBeTrue();
    [Fact] void should_export_http_protobuf() => _requests.All(request => request.ContentType == "application/x-protobuf").ShouldBeTrue();
    [Fact] void should_export_the_generated_counter_by_its_exact_name() => _successfulObservations.ShouldNotBeEmpty();
    [Fact] void should_export_the_hand_written_counter_by_its_exact_name() => _partitionFailures.ShouldNotBeEmpty();
    [Fact] void should_export_the_generated_description() => _successfulObservations.All(entry => entry.Metric.Description == "Number of successful observations per observer in a given event store and namespace").ShouldBeTrue();
    [Fact] void should_export_the_hand_written_description() => _partitionFailures.All(entry => entry.Metric.Description == "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt").ShouldBeTrue();
    [Fact] void should_decode_an_empty_generated_unit() => _successfulObservations.All(entry => entry.Metric.Unit.Length == 0).ShouldBeTrue();
    [Fact] void should_decode_an_empty_hand_written_unit() => _partitionFailures.All(entry => entry.Metric.Unit.Length == 0).ShouldBeTrue();
    [Fact] void should_export_the_generated_counter_as_a_monotonic_sum() => _successfulObservations.All(entry => entry.Metric.Sum.IsMonotonic).ShouldBeTrue();
    [Fact] void should_export_the_hand_written_counter_as_a_monotonic_sum() => _partitionFailures.All(entry => entry.Metric.Sum.IsMonotonic).ShouldBeTrue();
    [Fact] void should_export_an_integer_generated_value() => _successfulObservations.All(entry => entry.Point.AsInt.HasValue).ShouldBeTrue();
    [Fact] void should_export_an_integer_hand_written_value() => _partitionFailures.All(entry => entry.Point.AsInt.HasValue).ShouldBeTrue();
    [Fact] void should_export_one_successful_observation() => _successfulObservations.All(entry => entry.Point.AsInt == 1).ShouldBeTrue();
    [Fact] void should_export_one_partition_failure() => _partitionFailures.All(entry => entry.Point.AsInt == 1).ShouldBeTrue();
    [Fact] void should_export_exactly_the_four_generated_dimensions() => _successfulObservations.All(entry => HasExpectedTags(entry.Point)).ShouldBeTrue();
    [Fact] void should_export_exactly_the_four_hand_written_dimensions() => _partitionFailures.All(entry => HasExpectedTags(entry.Point)).ShouldBeTrue();
}

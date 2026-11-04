// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Transactions;
using Cratis.Traces;
using ClientReactors = Cratis.Chronicle.Reactors.Reactors;
using ClientReducers = Cratis.Chronicle.Reducers.Reducers;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ClientSpanCompatibility;

public class when_starting_client_spans : Specification
{
    static readonly string[] _legacyNames = ["client.event_sequence.append", "client.event_sequence.append_many", "client.reactor.handle", "client.reducer.handle", "client.unit_of_work.commit", "client.unit_of_work.rollback"];
    static readonly string[] _conventionNames = ["cratis.chronicle.client.event_sequence.append", "cratis.chronicle.client.event_sequence.append_many", "cratis.chronicle.client.reactor.handle", "cratis.chronicle.client.reducer.handle", "cratis.chronicle.client.unit_of_work.commit", "cratis.chronicle.client.unit_of_work.rollback"];
    static readonly string[][] _legacyKeys =
    [
        ["event_store_name", "namespace_name", "event_sequence_id", "event_source_type", "event_source_id"],
        ["event_store_name", "namespace_name", "event_sequence_id"],
        ["event_store_name", "namespace_name", "event_sequence_id", "reactor_id"],
        ["event_store_name", "namespace_name", "event_sequence_id", "reducer_id"],
        ["correlation_id"],
        ["correlation_id"]
    ];
    static readonly string[][] _conventionKeys =
    [
        ["cratis.event_store.name", "cratis.event_store.namespace", "cratis.event_sequence.id", "cratis.event_source.type", "cratis.event_source.id", "event_store_name", "namespace_name", "event_sequence_id", "event_source_type", "event_source_id"],
        ["cratis.event_store.name", "cratis.event_store.namespace", "cratis.event_sequence.id", "event_store_name", "namespace_name", "event_sequence_id"],
        ["cratis.event_store.name", "cratis.event_store.namespace", "cratis.event_sequence.id", "cratis.observer.id", "cratis.observer.type", "event_store_name", "namespace_name", "event_sequence_id", "reactor_id"],
        ["cratis.event_store.name", "cratis.event_store.namespace", "cratis.event_sequence.id", "cratis.observer.id", "cratis.observer.type", "event_store_name", "namespace_name", "event_sequence_id", "reducer_id"],
        ["cratis.correlation_id", "correlation_id"],
        ["cratis.correlation_id", "correlation_id"]
    ];
    readonly ActivitySource _source = new($"client-span-spec-{Guid.NewGuid():N}");
    readonly List<Activity> _spans = [];
    readonly List<KeyValuePair<string, object?>[]> _started = [];
    ActivityListener _listener;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = source => ReferenceEquals(source, _source),
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _started.Add(activity.TagObjects.ToArray()),
            ActivityStopped = activity => _spans.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    void Because()
    {
        var events = Source<EventSequence>();
        var reactors = Source<ClientReactors>();
        var reducers = Source<ClientReducers>();
        var units = Source<UnitOfWork>();
        foreach (var mode in new[] { new ChronicleTelemetryOptions().SpanNames, ClientSpanNames.Convention })
        {
            events.Append(new("store", "namespace", "log", "customer"), mode, "source").Dispose();
            events.AppendMany(new("store", "namespace", "log"), mode).Dispose();
            reactors.Handle(new("store", "namespace", "log", "reactor"), mode).Dispose();
            reducers.Handle(new("store", "namespace", "log", "reducer"), mode).Dispose();
            units.Commit("correlation", mode).Dispose();
            units.Rollback("correlation", mode).Dispose();
        }
    }

    void Destroy()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    IActivitySource<T> Source<T>()
    {
        var source = Substitute.For<IActivitySource<T>>();
        source.ActualSource.Returns(_source);
        return source;
    }

    [Fact] void should_have_shared_attributes_when_convention_spans_start() => _started.Skip(6).All(tags => tags.Any(tag => tag.Key.StartsWith("cratis.", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_set_legacy_attributes_after_start_like_main() => _started.Take(6).All(tags => tags.Length == 0).ShouldBeTrue();
    [Fact] void should_never_start_with_an_event_source_id() => _started.TrueForAll(tags => tags.All(tag => !Equals(tag.Value, "source"))).ShouldBeTrue();
    [Fact] void should_default_to_legacy_names() => _spans.Take(6).Select(_ => _.OperationName).ShouldEqual(_legacyNames);
    [Fact] void should_default_to_legacy_display_names() => _spans.Take(6).Select(_ => _.DisplayName).ShouldEqual(_legacyNames);
    [Fact] void should_opt_in_to_convention_operation_names() => _spans.Skip(6).Select(_ => _.OperationName).ShouldEqual(_conventionNames);
    [Fact] void should_opt_in_to_convention_display_names() => _spans.Skip(6).Select(_ => _.DisplayName).ShouldEqual(_conventionNames);
    [Fact] void should_keep_the_kinds() => _spans.Select(_ => _.Kind).ShouldEqual([ActivityKind.Client, ActivityKind.Client, ActivityKind.Consumer, ActivityKind.Consumer, ActivityKind.Client, ActivityKind.Client, ActivityKind.Client, ActivityKind.Client, ActivityKind.Consumer, ActivityKind.Consumer, ActivityKind.Client, ActivityKind.Client]);
    [Fact] void should_keep_legacy_event_store_tags_in_both_modes() => _spans.Where(_ => _.OperationName.Contains("event_sequence")).All(_ => Equals(_.GetTagItem("event_store_name"), "store")).ShouldBeTrue();
    [Fact] void should_add_shared_event_store_tags_in_convention_mode() => _spans.Skip(6).Where(_ => _.OperationName.Contains("event_sequence")).All(_ => Equals(_.GetTagItem("cratis.event_store.name"), "store")).ShouldBeTrue();
    [Fact] void should_keep_both_correlation_keys_in_convention_mode() => _spans.Skip(6).Where(_ => _.OperationName.Contains("unit_of_work")).All(_ => Equals(_.GetTagItem("correlation_id"), "correlation") && Equals(_.GetTagItem("cratis.correlation_id"), "correlation")).ShouldBeTrue();
    [Fact] void should_add_reactor_identity_in_convention_mode() => _spans.Skip(6).Where(_ => _.OperationName.Contains("reactor")).All(_ => Equals(_.GetTagItem("cratis.observer.id"), "reactor") && Equals(_.GetTagItem("cratis.observer.type"), "reactor")).ShouldBeTrue();
    [Fact] void should_add_reducer_identity_in_convention_mode() => _spans.Skip(6).Where(_ => _.OperationName.Contains("reducer")).All(_ => Equals(_.GetTagItem("cratis.observer.id"), "reducer") && Equals(_.GetTagItem("cratis.observer.type"), "reducer")).ShouldBeTrue();

    [Fact]
    void should_pin_main_legacy_tag_keys_for_every_operation()
    {
        foreach (var (span, keys) in _spans.Take(6).Zip(_legacyKeys))
        {
            span.TagObjects.Select(_ => _.Key).Order().ShouldEqual(keys.Order());
        }
    }

    [Fact]
    void should_pin_convention_tag_keys_for_every_operation()
    {
        foreach (var (span, keys) in _spans.Skip(6).Zip(_conventionKeys))
        {
            span.TagObjects.Select(_ => _.Key).Order().ShouldEqual(keys.Order());
        }
    }
}

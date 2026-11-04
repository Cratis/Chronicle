// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Transactions;
using Cratis.Traces;
using ClientReactors = Cratis.Chronicle.Reactors.Reactors;
using ClientReducers = Cratis.Chronicle.Reducers.Reducers;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ClientSpanCompatibility;

[Collection(ListenerFreeSpansCollection.Name)]
public class when_no_one_listens : Specification
{
    readonly ActivitySource _source = new($"listener-free-{Guid.NewGuid():N}");
    IActivitySource<EventSequence> _events;
    IActivitySource<ClientReactors> _reactors;
    IActivitySource<ClientReducers> _reducers;
    IActivitySource<UnitOfWork> _units;
    Activity? _parent;
    long _allocated;
    bool _allActivitiesWereNull = true;

    void Establish()
    {
        _events = new Cratis.Traces.ActivitySource<EventSequence>(_source);
        _reactors = new Cratis.Traces.ActivitySource<ClientReactors>(_source);
        _reducers = new Cratis.Traces.ActivitySource<ClientReducers>(_source);
        _units = new Cratis.Traces.ActivitySource<UnitOfWork>(_source);
        _parent = Activity.Current;
        _source.HasListeners().ShouldBeFalse();
    }

    void Because()
    {
        StartSpans(ClientSpanNames.Legacy);
        StartSpans(ClientSpanNames.Convention);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++)
        {
            StartSpans(ClientSpanNames.Legacy);
            StartSpans(ClientSpanNames.Convention);
        }
        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    void Destroy() => _source.Dispose();

    void StartSpans(ClientSpanNames mode)
    {
        using var append = _events.Append(new("store", "namespace", "log", "customer"), mode, "source");
        using var appendMany = _events.AppendMany(new("store", "namespace", "log"), mode);
        using var reactor = _reactors.Handle(new("store", "namespace", "log", "reactor"), mode);
        using var reducer = _reducers.Handle(new("store", "namespace", "log", "reducer"), mode);
        using var commit = _units.Commit("correlation", mode);
        using var rollback = _units.Rollback("correlation", mode);
        _allActivitiesWereNull &= append.Activity is null && appendMany.Activity is null && reactor.Activity is null && reducer.Activity is null && commit.Activity is null && rollback.Activity is null;
    }

    [Fact] void should_create_no_activities_in_either_mode() => _allActivitiesWereNull.ShouldBeTrue();
    [Fact] void should_allocate_nothing_per_span_in_either_mode_after_static_initialization() => _allocated.ShouldEqual(0);
    [Fact] void should_leave_the_parent_unchanged() => Activity.Current.ShouldEqual(_parent);
    [Fact] void should_return_before_reading_legacy_metadata() => ClientSpanCompatibility.Start(_events, null!, ClientSpanNames.Legacy, default).Activity.ShouldBeNull();
    [Fact] void should_return_before_reading_convention_metadata() => ClientSpanCompatibility.Start(_events, null!, ClientSpanNames.Convention, default).Activity.ShouldBeNull();
    [Fact] void should_return_when_the_source_is_null() => ClientSpanCompatibility.Start<EventSequence>(null!, null!, ClientSpanNames.Legacy, default).Activity.ShouldBeNull();
    [Fact] void should_return_when_the_actual_source_is_null() => ClientSpanCompatibility.Start(Substitute.For<IActivitySource<EventSequence>>(), null!, ClientSpanNames.Convention, default).Activity.ShouldBeNull();
}

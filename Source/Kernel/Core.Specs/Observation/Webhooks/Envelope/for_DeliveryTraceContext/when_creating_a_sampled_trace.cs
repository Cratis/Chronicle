// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_DeliveryTraceContext;

public class when_creating_a_sampled_trace : Specification
{
    string _traceParent;
    string _activityId;
    object _correlation;
    Activity _afterDispose;
    Activity _before;

    void Because()
    {
        _before = Activity.Current!;
        Activity.Current = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ChronicleActivity.SourceName,
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using (var trace = DeliveryTraceContext.Create(CorrelationId.NotSet))
        {
            _traceParent = trace.TraceParent;
            _activityId = Activity.Current!.Id!;
            _correlation = Activity.Current.GetTagItem("chronicle.correlation_id")!;
        }
        _afterDispose = Activity.Current!;
        Activity.Current = _before;
    }

    [Fact] void should_use_the_created_activity() => _traceParent.ShouldEqual(_activityId);
    [Fact] void should_tag_the_correlation_without_faking_a_trace_id() => _correlation.ShouldEqual(CorrelationId.NotSet.Value.ToString());
    [Fact] void should_dispose_the_created_activity() => _afterDispose.ShouldBeNull();
}

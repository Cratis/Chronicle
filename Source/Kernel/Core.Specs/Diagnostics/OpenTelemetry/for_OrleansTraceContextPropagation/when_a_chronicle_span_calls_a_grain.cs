// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.given;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OrleansTraceContextPropagation;

[Collection(nameof(orleans_activity_source_collection))]
public class when_a_chronicle_span_calls_a_grain : given.a_kernel_tracer_without_orleans_sources
{
    void Because()
    {
        Caller = ChronicleActivity.Source.StartActivity("caller");
        CallGrain(() => ChronicleActivity.Source.StartActivity("callee"));
    }

    [Fact] void should_continue_the_callers_trace() => Callee.TraceId.ShouldEqual(Caller.TraceId);
    [Fact] void should_record_the_callee_span() => Callee.Recorded.ShouldBeTrue();
    [Fact] void should_create_the_orleans_client_activity_for_propagation() => Client.ShouldNotBeNull();
    [Fact] void should_export_the_callee_span() => Exported.ShouldContain(Callee);
    [Fact] void should_not_export_orleans_spans() => Exported.Exists(activity => OrleansActivitySources.All.Contains(activity.Source.Name)).ShouldBeFalse();
}

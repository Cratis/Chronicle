// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.given;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OrleansTraceContextPropagation;

[Collection(nameof(orleans_activity_source_collection))]
public class when_the_calling_trace_is_not_sampled : given.a_kernel_tracer_without_orleans_sources
{
    void Because()
    {
        Caller = new Activity("caller")
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None).Start();
        CallGrain(() => ChronicleActivity.Source.StartActivity("callee"));
    }

    [Fact] void should_still_propagate_the_trace() => Client.TraceId.ShouldEqual(Caller.TraceId);
    [Fact] void should_keep_the_callee_in_the_callers_trace() => (Callee is null || Callee.TraceId == Caller.TraceId).ShouldBeTrue();
    [Fact] void should_not_record_the_callee_span() => (Callee?.Recorded != true).ShouldBeTrue();
}

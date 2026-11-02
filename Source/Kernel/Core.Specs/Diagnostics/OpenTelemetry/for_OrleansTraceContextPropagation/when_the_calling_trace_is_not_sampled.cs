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
    [Fact] void should_not_create_a_callee_span() => Callee.ShouldBeNull();
}

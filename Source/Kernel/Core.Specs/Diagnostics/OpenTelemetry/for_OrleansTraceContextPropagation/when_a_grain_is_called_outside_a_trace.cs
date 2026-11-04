// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.given;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OrleansTraceContextPropagation;

[Collection(nameof(orleans_activity_source_collection))]
public class when_a_grain_is_called_outside_a_trace : given.a_kernel_tracer_without_orleans_sources
{
    void Because() => CallGrain(() => ChronicleActivity.Source.StartActivity("callee"));

    [Fact] void should_not_create_an_orleans_span() => Client.ShouldBeNull();
}

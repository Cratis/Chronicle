// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_instrumentation;

[Collection(nameof(given.orleans_activity_source_collection))]
public class and_orleans_tracing_is_not_requested : Specification
{
    TracerProvider _provider;
    ActivitySource _orleans;
    Activity _orleans_span;
    Activity _chronicle_span;

    void Establish()
    {
        _provider = Sdk.CreateTracerProviderBuilder().AddChronicleInstrumentation().Build();
        _orleans = new("Microsoft.Orleans.Application");
    }

    void Because()
    {
        _orleans_span = _orleans.StartActivity("probe");
        _orleans_span?.Stop();
        _chronicle_span = ChronicleActivity.Source.StartActivity("probe");
        _chronicle_span?.Stop();
    }

    void Destroy()
    {
        _orleans.Dispose();
        _provider.Dispose();
    }

    [Fact] void should_not_create_an_orleans_span() => _orleans_span.ShouldBeNull();
    [Fact] void should_record_chronicle_spans() => _chronicle_span.Recorded.ShouldBeTrue();
}

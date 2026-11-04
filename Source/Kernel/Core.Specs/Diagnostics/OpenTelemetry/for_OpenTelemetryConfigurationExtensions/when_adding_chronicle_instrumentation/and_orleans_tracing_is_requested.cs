// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_instrumentation;

[Collection(nameof(given.orleans_activity_source_collection))]
public class and_orleans_tracing_is_requested : Specification
{
    TracerProvider _provider;
    ActivitySource _runtime;
    ActivitySource _application;
    Activity _runtime_span;
    Activity _application_span;

    void Establish()
    {
        _provider = Sdk.CreateTracerProviderBuilder().AddChronicleInstrumentation(true).Build();
        _runtime = new(OrleansActivitySources.Runtime);
        _application = new(OrleansActivitySources.Application);
    }

    void Because()
    {
        _runtime_span = _runtime.StartActivity("probe");
        _runtime_span?.Stop();
        _application_span = _application.StartActivity("probe");
        _application_span?.Stop();
    }

    void Destroy()
    {
        _runtime.Dispose();
        _application.Dispose();
        _provider.Dispose();
    }

    [Fact] void should_record_runtime_spans() => _runtime_span.Recorded.ShouldBeTrue();
    [Fact] void should_record_application_spans() => _application_span.Recorded.ShouldBeTrue();
}

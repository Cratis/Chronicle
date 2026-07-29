// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.AspNetCore.OpenTelemetry;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.AspNetCore.for_TracerProviderBuilderExtensions;

/// <summary>
/// An <see cref="ActivitySource"/> produces nothing until something listens to it by name, so registering the
/// client's source is the whole of what this extension does. Both halves are asserted in one spec because an
/// activity listener is process-wide, and separate spec classes would see each other's tracer provider.
/// </summary>
public class when_adding_chronicle_client_instrumentation : Specification
{
    static readonly ActivitySource _source = new(ClientActivity.SourceName);

    TracerProvider _provider;
    Activity _beforeAdding;
    Activity _afterAdding;

    void Because()
    {
        _beforeAdding = _source.StartActivity("some-client-operation");
        _provider = Sdk.CreateTracerProviderBuilder().AddCratisChronicleInstrumentation().Build();
        _afterAdding = _source.StartActivity("some-client-operation");
    }

    void Destroy()
    {
        _beforeAdding?.Dispose();
        _afterAdding?.Dispose();
        _provider?.Dispose();
    }

    [Fact] void should_not_record_client_activity_before_it_is_added() => _beforeAdding.ShouldBeNull();
    [Fact] void should_record_client_activity_once_it_is_added() => _afterAdding.ShouldNotBeNull();
    [Fact] void should_record_it_against_the_client_source() => _afterAdding.Source.Name.ShouldEqual(ClientActivity.SourceName);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OrleansTraceContextPropagation.given;

public class a_kernel_tracer_without_orleans_sources : Specification
{
    protected Activity Client;
    protected Activity Callee;
    protected Activity Caller;
    protected List<Activity> Exported = [];
    TracerProvider _provider;
    OrleansTraceContextPropagation _propagation;
    ActivitySource _source;

    async Task Establish()
    {
        _provider = Sdk.CreateTracerProviderBuilder().AddChronicleInstrumentation(false)
            .AddProcessor(new SimpleActivityExportProcessor(new OpenTelemetry.given.exported_activities(Exported))).Build();
        _source = new(OrleansActivitySources.Application);
        _propagation = new();
        await _propagation.StartAsync(CancellationToken.None);
    }

    protected void CallGrain(Func<Activity?> inGrain)
    {
        // Mirrors Orleans v10.4.0 ActivityPropagationGrainCallFilter: inject only when a client
        // activity exists, then extract in a grain turn without the caller's ExecutionContext.
        var propagator = DistributedContextPropagator.Current;
        var carrier = new Dictionary<string, string>();
        using (Client = _source.StartActivity("grain", ActivityKind.Client))
        {
            if (Client is not null)
                propagator.Inject(Client, carrier, (state, name, value) => ((Dictionary<string, string>)state)[name] = value);
        }

        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            propagator.ExtractTraceIdAndState(carrier, Get, out var traceParent, out var traceState);
            using var server = ActivityContext.TryParse(traceParent, traceState, true, out var parent)
                ? _source.CreateActivity("grain", ActivityKind.Server, parent)?.Start()
                : null;
            Callee = inGrain();
            Callee?.Stop();
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    async Task Destroy()
    {
        Caller?.Dispose();
        await _propagation.StopAsync(CancellationToken.None);
        _propagation.Dispose();
        _source.Dispose();
        _provider.Dispose();
    }

    static void Get(object? carrier, string name, out string? value, out IEnumerable<string>? values)
    {
        value = carrier is Dictionary<string, string> headers && headers.TryGetValue(name, out var header) ? header : null;
        values = null;
    }
}

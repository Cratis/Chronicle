// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.Benchmarks.Telemetry;

/// <summary>
/// How the TracerProvider and the Orleans trace propagation are configured.
/// </summary>
public enum TracingConfiguration
{
    /// <summary>Before #4607: the TracerProvider listens to both Orleans sources, no propagation listener.</summary>
    Base,

    /// <summary>After #4607, OrleansTracing=false: Chronicle's provider without Orleans sources plus <see cref="OrleansTraceContextPropagation"/>.</summary>
    HeadTracingOff,

    /// <summary>After #4607, OrleansTracing=true: the provider as <c>AddChronicleTelemetry</c> builds it with tracing on.</summary>
    HeadTracingOn
}

/// <summary>
/// What the caller of the grain has as its current activity.
/// </summary>
public enum CallerContext
{
    /// <summary>No current <see cref="Activity"/>.</summary>
    Untraced,

    /// <summary>A current activity from a sampled (Recorded) parent.</summary>
    SampledParent,

    /// <summary>A current activity from an unsampled parent.</summary>
    UnsampledParent
}

/// <summary>
/// Measures the per-call cost of Orleans' activity propagation grain call filters under the telemetry configurations
/// before and after #4607. The grain call is modeled in process: it reproduces the work of Orleans 10.4's
/// <c>ActivityPropagationGrainCallFilter</c> (outgoing: start a client activity on the Orleans application source and
/// inject the context; incoming: extract the context without the caller's ExecutionContext, start a server activity -
/// a root one when no context arrived, as Orleans does - and set the rpc tags when all data is requested) rather than
/// running a silo. <see cref="GrainCallWithCalleeSpan"/> adds the callee side: the grain's own Chronicle span, which is
/// where Base and head differ for an unsampled caller (Base drops the context, so the callee records a root trace).
/// </summary>
[MemoryDiagnoser]
public class OrleansGrainCallTracing : IDisposable
{
    static readonly ActivitySource _source = new(OrleansActivitySources.Application);
    static readonly Action<Dictionary<string, string>, string, string> _set = (carrier, name, value) => carrier[name] = value;
    static readonly DistributedContextPropagator.PropagatorGetterCallback _get = Get;

    static readonly ActivitySource _callerSource = new("Cratis.Chronicle.Benchmarks.Caller");
    ActivityListener? _callerListener;
    TracerProvider? _provider;
    OrleansTraceContextPropagation? _propagation;
    Activity? _caller;
    Activity? _verifiedClient;
    Activity? _verifiedCallee;
    bool _verifying;
    int _sequence;

    /// <summary>
    /// Gets or sets the telemetry configuration under test.
    /// </summary>
    [Params(TracingConfiguration.Base, TracingConfiguration.HeadTracingOff, TracingConfiguration.HeadTracingOn)]
    public TracingConfiguration Configuration { get; set; }

    /// <summary>
    /// Gets or sets the caller's trace context.
    /// </summary>
    [Params(CallerContext.Untraced, CallerContext.SampledParent, CallerContext.UnsampledParent)]
    public CallerContext Caller { get; set; }

    /// <summary>
    /// Builds the tracer provider (and the propagation listener for the head, tracing-off case).
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        // ParentBased(AlwaysOn) is the OpenTelemetry default sampler AddChronicleTelemetry relies on.
        var builder = Sdk.CreateTracerProviderBuilder();
        switch (Configuration)
        {
            case TracingConfiguration.Base:
                builder.AddSource(ChronicleActivitySourceName).AddSource(OrleansActivitySources.All);
                break;
            case TracingConfiguration.HeadTracingOff:
                builder.AddChronicleInstrumentation(false);
                break;
            default:
                builder.AddChronicleInstrumentation(true);
                break;
        }

#pragma warning disable CA2000 // Ownership of the processor passes to the TracerProvider, which disposes it.
        _provider = builder.AddProcessor(new SimpleActivityExportProcessor(new NoOpExporter())).Build();
#pragma warning restore CA2000

        if (Configuration == TracingConfiguration.HeadTracingOff)
        {
            _propagation = new();
            await _propagation.StartAsync(CancellationToken.None);
        }

        if (Caller == CallerContext.Untraced)
        {
            VerifyEntry(default);
            VerifyCallee();
            return;
        }

        var flags = Caller == CallerContext.SampledParent ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
        var parent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), flags, isRemote: true);
        _callerListener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, _callerSource),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => options.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.PropagationData
        };
        ActivitySource.AddActivityListener(_callerListener);
        _caller = _callerSource.StartActivity("caller", ActivityKind.Server, parent);
        VerifyEntry(parent);
        VerifyCallee();
    }

    /// <summary>
    /// Disposes the provider and propagation listener.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Activity.Current = null;
        _caller?.Dispose();
        Activity.Current = null;
        _callerListener?.Dispose();
        _propagation?.Dispose();
        _provider?.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose() => Cleanup();

    /// <summary>
    /// One modeled grain call: outgoing filter then incoming filter.
    /// </summary>
    /// <returns>The value the grain would return.</returns>
    [Benchmark]
    public int GrainCall() => Call(calleeSpan: false);

    /// <summary>
    /// One modeled grain call whose grain starts its own Chronicle span, as kernel grains do.
    /// </summary>
    /// <returns>The value the grain would return.</returns>
    [Benchmark]
    public int GrainCallWithCalleeSpan() => Call(calleeSpan: true);

    int Call(bool calleeSpan)
    {
        // Activity.Current is AsyncLocal and does not flow from the async GlobalSetup to the workload thread,
        // so establish the caller on the thread that runs the call, as it is on a real grain call. Same cost for every configuration.
        Activity.Current = _caller;
        var current = _caller;
        var carrier = new Dictionary<string, string>(2);

        // Outgoing filter.
        using (var client = _source.StartActivity("IPing/Ping", ActivityKind.Client))
        {
            if (_verifying) _verifiedClient = client;
            if (client is not null)
            {
                if (client.IsAllDataRequested) SetRpcTags(client);
                DistributedContextPropagator.Current.Inject(client, carrier, (state, name, value) => _set((Dictionary<string, string>)state!, name, value));
            }
        }

        // Incoming filter, in a grain turn that does not see the caller's ExecutionContext.
        Activity.Current = null;
        try
        {
            using var server = StartServer(carrier);
            if (server is { IsAllDataRequested: true }) SetRpcTags(server);
            if (calleeSpan)
            {
                using var callee = ChronicleActivity.Source.StartActivity("IPing.Ping");
                if (_verifying) _verifiedCallee = callee;
                callee?.SetTag("chronicle.benchmark", "callee");
            }

            return ++_sequence;
        }
        finally
        {
            Activity.Current = current;
        }
    }

    void VerifyEntry(ActivityContext parent)
    {
        Activity.Current = _caller;
        var current = Activity.Current;
        var recorded = Caller == CallerContext.SampledParent;
        if (Caller == CallerContext.Untraced)
        {
            if (current is not null) throw new InvalidOperationException("Untraced caller entered GrainCall with a current activity.");
        }
        else if (current is null || current.TraceId != parent.TraceId || current.Recorded != recorded)
        {
            throw new InvalidOperationException($"{Caller} caller did not enter GrainCall with the intended parent (current={current?.TraceId.ToString() ?? "none"}, recorded={current?.Recorded}, expected recorded={recorded}).");
        }

        _verifying = true;
        _verifiedClient = null;
        try
        {
            GrainCall();
        }
        finally
        {
            _verifying = false;
        }

        var client = _verifiedClient;
        _verifiedClient = null;
        if (Caller == CallerContext.Untraced) return;
        if (client is null)
        {
            if (recorded) throw new InvalidOperationException($"{Configuration}/{Caller}: GrainCall created no client carrier activity.");
            return;
        }

        var expectAllData = recorded && Configuration != TracingConfiguration.HeadTracingOff;
        if (client.TraceId != parent.TraceId || client.Recorded != recorded || (recorded && client.IsAllDataRequested != expectAllData))
        {
            throw new InvalidOperationException($"{Configuration}/{Caller}: client carrier activity has TraceId match={client.TraceId == parent.TraceId}, Recorded={client.Recorded} (expected {recorded}), IsAllDataRequested={client.IsAllDataRequested} (expected {expectAllData}).");
        }
    }

    static string ChronicleActivitySourceName => ChronicleActivity.SourceName;

    static Activity? StartServer(Dictionary<string, string> carrier)
    {
        DistributedContextPropagator.Current.ExtractTraceIdAndState(carrier, _get, out var traceParent, out var traceState);
        if (string.IsNullOrEmpty(traceParent)) return _source.CreateActivity("IPing/Ping", ActivityKind.Server)?.Start();
        return ActivityContext.TryParse(traceParent, traceState, true, out var parent)
            ? _source.CreateActivity("IPing/Ping", ActivityKind.Server, parent)?.Start()
            : _source.CreateActivity("IPing/Ping", ActivityKind.Server, traceParent)?.Start();
    }

    void VerifyCallee()
    {
        Activity.Current = _caller;
        _verifying = true;
        _verifiedCallee = null;
        try
        {
            Call(calleeSpan: true);
        }
        finally
        {
            _verifying = false;
            Activity.Current = _caller;
        }

        // Only head with tracing off honors an unsampled caller on the callee; every other case records the callee span.
        var expectRecorded = !(Configuration == TracingConfiguration.HeadTracingOff && Caller == CallerContext.UnsampledParent);
        var recorded = _verifiedCallee?.Recorded == true;
        _verifiedCallee = null;
        if (recorded != expectRecorded)
        {
            throw new InvalidOperationException($"{Configuration}/{Caller}: callee span recorded={recorded}, expected {expectRecorded}.");
        }
    }

    static void SetRpcTags(Activity activity)
    {
        activity.SetTag("rpc.system", "orleans.rpc");
        activity.SetTag("rpc.service", "Cratis.Chronicle.Benchmarks.IPing");
        activity.SetTag("rpc.method", "Ping");
        activity.SetTag("orleans.grain.id", string.Concat("ping/", "0f3c1d2e4b5a69788796a5b4c3d2e1f0"));
    }

    static void Get(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues)
    {
        fieldValues = null;
        fieldValue = ((Dictionary<string, string>)carrier!).TryGetValue(fieldName, out var value) ? value : null;
    }

    sealed class NoOpExporter : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch) => ExportResult.Success;
    }
}

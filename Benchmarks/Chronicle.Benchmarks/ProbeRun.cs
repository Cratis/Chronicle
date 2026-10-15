// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks;

internal sealed record ProbePayload(int Value, DateTimeOffset Timestamp);

internal sealed class ProbeRun(string scenario, string store)
{
    internal readonly ConcurrentDictionary<ProbePosition, ProbePayload> Appended = new();
    internal readonly ProbeDelivery Delivery = new() { Noop = scenario.Split(':').Contains("noop") };
    internal readonly Stopwatch Clock = new();
    internal long Started;
    internal int SubmittedCount;
    internal TimeSpan CpuStart;

    public string Baseline => "d5d0eea66de49e36e197910a826ae8cffc6eebbc";
    public string Scenario => scenario;
    public string AppendConcurrency => "ConcurrencyScope.None: unconditional load, no optimistic retries";
    public string EventStore => store;
    public string Status { get; set; } = "DID_NOT_FINISH";
    public string? Reason { get; set; }
    public int DeadlineSeconds { get; set; } = int.Parse(Environment.GetEnvironmentVariable("PROBE_DEADLINE_SECONDS") ?? "200");
    public int QuiescenceMilliseconds => 2000;
    public int Requested { get; set; }
    public int Submitted { get => Volatile.Read(ref SubmittedCount); set => SubmittedCount = value; }
    public int Expected => Appended.Count;
    public bool AppendFinished { get; set; }
    public bool ObserverListPassed { get; set; }
    public string[] BeforeObservers { get; set; } = [];
    public string[] Observers { get; set; } = [];
    public string[] EndObservers { get; set; } = [];
    public string[] PersistedObserverState { get; set; } = [];
    public string[] InitialObserverState { get; set; } = [];
    public double? SystemDurableMs { get; set; }
    public string ProbeObserverId => Scenario.Contains("projection") ? typeof(BenchmarkProjection).FullName! : "probe-4748";
    public string[] PersistedJobs { get; set; } = [];
    public string[] JobWriteSamples { get; set; } = [];
    public string[] CompletedJobEvidence { get; set; } = [];
    public Dictionary<string, string[]>? ProjectionDocuments { get; set; }
    public int? ProjectionRows { get; set; }
    public bool? ProjectionValuesMatch { get; set; }
    public ProbeAccounting? Accounting { get; set; }
    public double? EffectMs { get; set; }
    public double? DurableMs { get; set; }
    public double? JobMs { get; set; }
    public double? DurableGapMs => DurableMs - EffectMs;
    public double? EventsPerSecond => EffectMs > 0 ? Expected * 1000.0 / EffectMs : null;
    public double? PartialEventsPerSecond => !Scenario.Contains("projection") && !Scenario.StartsWith("appendonly") && Clock.ElapsedMilliseconds > 0
        ? Accounting?.DistinctDelivered * 1000.0 / Clock.Elapsed.TotalMilliseconds : null;
    public double AppendMs { get; set; }
    public double WallMs { get; set; }
    public double HandlerBusyMs => Delivery.BusyMs;
    public double ClientCpuMs { get; set; }
    public double? ClientCpuCores => WallMs > 0 ? ClientCpuMs / WallMs : null;
    public JsonElement? ProfileStart { get; set; }
    public JsonElement? Mongo { get; set; }
    public string AccountingMethod => Scenario.StartsWith("appendonly") ? "no probe observer: successful append results only; kernel observers remain"
        : Scenario.Contains("projection") ? "sink final payload per key at highest acknowledged sequence, not per-event effects"
        : "exact acknowledged append-result membership; mandatory lock/timestamp instrumentation even in noop";

    internal void Snapshot()
    {
        WallMs = Clock.Elapsed.TotalMilliseconds;
        if (!Scenario.Contains("projection") && !Scenario.StartsWith("appendonly"))
        {
            Accounting = Delivery.Snapshot(Appended.Keys.ToArray(), Started);
            EffectMs = Accounting.EffectMs;
        }
    }
}

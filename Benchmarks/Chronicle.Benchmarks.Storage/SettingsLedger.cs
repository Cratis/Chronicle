// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Everything needed to decide whether two results can be compared: the provider and server, the durability it ran
/// with, the workload shape, the hardware, and how loaded the machine was. Written next to every result as `ledger.json`.
/// </summary>
/// <param name="Provider">The provider name.</param>
/// <param name="Profile">The profile name.</param>
/// <param name="ImageTag">The container image and tag, or `embedded` for SQLite.</param>
/// <param name="ImageDigest">The image digest, or `n/a`.</param>
/// <param name="ServerVersion">The server version as the server reports it.</param>
/// <param name="Topology">The server topology, for example `single-node replica set`.</param>
/// <param name="Durability">The server's durability settings, named as the server names them.</param>
/// <param name="DurabilityClass">The class from <see cref="DurabilityClassifier"/>; equal classes are comparable.</param>
/// <param name="ChronicleCommit">The Chronicle commit the harness was built from.</param>
/// <param name="BatchSizes">The batch sizes measured.</param>
/// <param name="Concurrency">The writer counts measured.</param>
/// <param name="PayloadBytes">The event payload size.</param>
/// <param name="EventCounts">The seeded event counts.</param>
/// <param name="ConnectionPoolSize">The connection pool size of the client.</param>
/// <param name="SnapshotInterval">The event sequence snapshot interval the activation workload depends on.</param>
/// <param name="CountersEnabled">Whether command counting was on (it adds overhead).</param>
/// <param name="Hardware">The machine.</param>
/// <param name="LoadAtStart">The load average when the run started.</param>
/// <param name="LoadAtEnd">The load average when the run ended.</param>
/// <param name="MaxLoad">The load limit that was applied.</param>
/// <param name="StartedUtc">When the run started.</param>
public sealed record SettingsLedger(
    string Provider,
    string Profile,
    string ImageTag,
    string ImageDigest,
    string ServerVersion,
    string Topology,
    IReadOnlyDictionary<string, string> Durability,
    string DurabilityClass,
    string ChronicleCommit,
    IReadOnlyList<int> BatchSizes,
    IReadOnlyList<int> Concurrency,
    int PayloadBytes,
    IReadOnlyList<int> EventCounts,
    int ConnectionPoolSize,
    int SnapshotInterval,
    bool CountersEnabled,
    HardwareDescription Hardware,
    LoadReading LoadAtStart,
    LoadReading? LoadAtEnd,
    double MaxLoad,
    DateTimeOffset StartedUtc)
{
    /// <summary>The JSON options every harness file is written with.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };

    /// <summary>Serializes the ledger.</summary>
    /// <returns>The JSON text.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Gets a copy with the end-of-run load.</summary>
    /// <param name="load">The load reading.</param>
    /// <returns>The completed ledger.</returns>
    public SettingsLedger WithLoadAtEnd(LoadReading load) => this with { LoadAtEnd = load };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Everything needed to decide whether two results can be compared: the provider and server, the durability it ran
/// with, the workload shape, the hardware, and how loaded the machine was. Written next to every result as `ledger.json`.
/// </summary>
/// <param name="Run">Which run this is.</param>
/// <param name="Server">The provider, its server and its durability.</param>
/// <param name="Workload">The workload shape, payload and client settings.</param>
/// <param name="Conditions">The machine and its load.</param>
public sealed record SettingsLedger(RunIdentity Run, ServerSetup Server, WorkloadSetup Workload, RunConditions Conditions)
{
    /// <summary>The JSON options every harness file is written with.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

    /// <summary>Gets the provider name.</summary>
    [JsonIgnore]
    public string Provider => Server.Provider;

    /// <summary>Gets the durability class.</summary>
    [JsonIgnore]
    public string DurabilityClass => Server.Durability.Class;

    /// <summary>Serializes the ledger.</summary>
    /// <returns>The JSON text.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Gets a copy with the end-of-run load.</summary>
    /// <param name="load">The load reading.</param>
    /// <returns>The completed ledger.</returns>
    public SettingsLedger WithLoadAtEnd(LoadReading load) => this with { Conditions = Conditions with { LoadAtEnd = load } };
}

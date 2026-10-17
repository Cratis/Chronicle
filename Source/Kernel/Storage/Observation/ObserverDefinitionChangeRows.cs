// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Shared row encoding and write predicates. Provider predicates still compare the stored version, state and position atomically.
/// </summary>
public static class ObserverDefinitionChangeRows
{
    static readonly JsonSerializerOptions _options = new() { Converters = { new ConceptAsJsonConverterFactory(), new KeyJsonConverter() } };

    /// <summary>
    /// Encodes one complete row, keeping mutable caller-owned collections outside storage.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="row">The row.</param>
    /// <returns>The encoded row.</returns>
    public static string Serialize<T>(ObserverDefinitionChangeRow<T> row) => JsonSerializer.Serialize(row, _options);

    /// <summary>
    /// Decodes a complete row.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="json">The stored row.</param>
    /// <returns>The row.</returns>
    public static ObserverDefinitionChangeRow<T> Deserialize<T>(string json) => JsonSerializer.Deserialize<ObserverDefinitionChangeRow<T>>(json, _options)!;

    /// <summary>
    /// Produces a fixed-size, ordinal identity from an unambiguous encoding of the complete key and family.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="key">The complete identity.</param>
    /// <returns>The opaque row identity and paging cursor.</returns>
    public static string Id<T>(ObserverDefinitionChangeRowKey key) => Hash(JsonSerializer.Serialize(new[] { Observer(key.Observer), key.Revision.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), typeof(T).Name, key.Id }));

    /// <summary>
    /// Encodes the complete observer identity for indexed, case-sensitive isolation.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <returns>The fixed-size observer identity.</returns>
    public static string Observer(ObserverKey observer) => Hash(JsonSerializer.Serialize(observer, _options));

    /// <summary>
    /// Validates row shape without granting permission to write it before initialization.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="row">The proposed row.</param>
    /// <returns>Whether its lifecycle and payload agree.</returns>
    public static bool IsValid<T>(ObserverDefinitionChangeRow<T> row) =>
        row.Version >= 0 && row.Key.Revision.Value > 0 && Enum.IsDefined(row.State) &&
        row.Position != EventSequenceNumber.Unavailable &&
        row.Data switch
        {
            ObserverProgressRecord progress => row.Position is not null && Enum.IsDefined(progress.Outcome) && row.State == ObserverDefinitionChangeRowState.Terminal == (progress.Outcome != ObserverWorkOutcome.Checkpoint),
            ObserverContinuation => row.Position is not null,
            ObserverRestartPlan => true,
            ObserverJobInventory job => row.State == ObserverDefinitionChangeRowState.Terminal == (job.Outcome is not null) && (job.Outcome is null || job.Outcome.Outcome != ObserverWorkOutcome.Checkpoint),
            _ => false
        };

    /// <summary>
    /// Validates a replacement's version before entering a provider's conditional update.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="row">The proposed row.</param>
    /// <param name="expectedVersion">The previously read version.</param>
    /// <returns>Whether the replacement can be attempted.</returns>
    public static bool IsReplacement<T>(ObserverDefinitionChangeRow<T> row, long expectedVersion) =>
        expectedVersion >= 0 && expectedVersion < long.MaxValue && row.Version == expectedVersion + 1 && IsValid(row);

    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
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
    public static string Id<T>(ObserverDefinitionChangeRowKey key) => Hash([Observer(key.Observer), Invariant(key.Revision.Value), ObserverDefinitionChangeRowFamilies.For<T>(), key.Id]);

    /// <summary>
    /// Encodes the complete observer identity for indexed, case-sensitive isolation.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <returns>The fixed-size observer identity.</returns>
    public static string Observer(ObserverKey observer) => Hash([observer.ObserverId.Value, observer.EventStore.Value, observer.Namespace.Value, observer.EventSequenceId.Value]);

    /// <summary>
    /// Encodes immutable execution identity for an atomic replacement predicate. Mutable outcomes are excluded.
    /// </summary>
    /// <typeparam name="T">The row payload.</typeparam>
    /// <param name="row">The row.</param>
    /// <returns>The fixed-size immutable payload identity.</returns>
    /// <exception cref="UnsupportedObserverDefinitionChangeRowFamily">The payload is not a row family.</exception>
    public static string Identity<T>(ObserverDefinitionChangeRow<T> row) => Hash(row.Data switch
    {
        ObserverProgressRecord progress => [progress.JobId.Value.ToString("D"), Partition(progress.Partition)],
        ObserverContinuation continuation => [continuation.JobId.Value.ToString("D"), Partition(continuation.Partition)],
        ObserverRestartPlan plan => [plan.JobId.Value.ToString("D"), Partition(plan.Partition), Invariant(plan.From.Value), Invariant(plan.To.Value), Scope(plan.Scope)],
        ObserverJobInventory job => [job.JobId.Value.ToString("D"), Invariant((int)job.Kind), job.JobType, job.Origin is null ? null : Encode([Invariant(job.Origin.IsRestartPlan), job.Origin.Id])],
        _ => throw new UnsupportedObserverDefinitionChangeRowFamily(typeof(T))
    });

    /// <summary>
    /// Rejects nonpositive limits and caps each storage page at 1,000 rows.
    /// </summary>
    /// <param name="limit">The requested limit.</param>
    /// <returns>The bounded positive limit.</returns>
    /// <exception cref="InvalidObserverDefinitionChangePageLimit">The limit is nonpositive.</exception>
    public static int PageLimit(int limit) => limit > 0 ? Math.Min(limit, 1000) : throw new InvalidObserverDefinitionChangePageLimit(limit);

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
            ObserverProgressRecord progress => row.Position is not null && Enum.IsDefined(progress.Outcome) &&
                row.State == (progress.Outcome == ObserverWorkOutcome.Checkpoint ? ObserverDefinitionChangeRowState.Started : ObserverDefinitionChangeRowState.Terminal),
            ObserverContinuation => row.Position is not null,
            ObserverRestartPlan => true,
            ObserverJobInventory job => (row.Version > 0 || row.State == ObserverDefinitionChangeRowState.Intended) &&
                Enum.IsDefined(job.Kind) && row.State == ObserverDefinitionChangeRowState.Terminal == (job.Outcome is not null) &&
                (job.Outcome is null || (Enum.IsDefined(job.Outcome.Outcome) && job.Outcome.Outcome != ObserverWorkOutcome.Checkpoint)),
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

    static string? Partition(Key? key) => key is null ? null : Encode(new[] { Invariant(key.Value) }
        .Concat(key.ArrayIndexers.All.SelectMany(indexer => new[] { indexer.ArrayProperty.ToString(), indexer.IdentifierProperty.ToString(), Invariant(indexer.Identifier) })));

    static string Scope(ObserverReaderScope scope) => Encode(new[] { Invariant(scope.IsWildcard) }
        .Concat(scope.EventTypes.SelectMany(type => new[] { type.Id.Value, Invariant(type.Generation.Value), Invariant(type.Tombstone) })));

    static string? Invariant(object? value) => value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);

    static string Hash(IEnumerable<string?> components) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Encode(components))));

    static string Encode(IEnumerable<string?> components)
    {
        var text = new StringBuilder();
        foreach (var component in components)
        {
            // Lengths count raw UTF-8 bytes; -1 distinguishes null from an empty component.
            var length = component is null ? -1 : Encoding.UTF8.GetByteCount(component);
            text.Append(length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(component).Append('|');
        }

        return text.ToString();
    }
}

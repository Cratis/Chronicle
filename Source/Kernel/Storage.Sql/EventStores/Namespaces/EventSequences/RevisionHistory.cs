// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Encodes optional row history without changing the released current-content format.
/// </summary>
internal static class RevisionHistory
{
    static readonly IReadOnlyDictionary<int, string> _noContent = new Dictionary<int, string>();
    static readonly IReadOnlyDictionary<int, EventHash> _noHashes = new Dictionary<int, EventHash>();

    static JsonSerializerOptions Options => EventEntryConverter.JsonOptions;

    /// <summary>
    /// Captures all original generations in the versioned envelope.
    /// </summary>
    /// <param name="entry">The event row before its first revision.</param>
    /// <returns>The serialized envelope.</returns>
    internal static string CaptureOriginal(EventEntry entry) =>
        JsonSerializer.Serialize(new { format = 1, content = JsonSerializer.Deserialize<JsonElement>(entry.Content), hashes = string.IsNullOrEmpty(entry.ContentHashes) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(entry.ContentHashes) }, Options);

    /// <summary>
    /// Appends a revision without losing earlier entries.
    /// </summary>
    /// <param name="history">The existing history.</param>
    /// <param name="revision">The new revision.</param>
    /// <returns>The serialized history.</returns>
    internal static string Append(string? history, EventRevisionEntry revision) =>
        JsonSerializer.Serialize(Read(history).Append(revision), Options);

    /// <summary>
    /// Reads the persisted history array.
    /// </summary>
    /// <param name="history">The nullable serialized history.</param>
    /// <returns>The recorded revisions.</returns>
    internal static IEnumerable<EventRevisionEntry> Read(string? history) =>
        string.IsNullOrEmpty(history) ? [] : JsonSerializer.Deserialize<EventRevisionEntry[]>(history, Options) ?? [];

    /// <summary>
    /// Checks that the last recorded revision still describes the current revision.
    /// </summary>
    /// <param name="entry">The current event row.</param>
    /// <returns>Whether its latest content and hash match the history.</returns>
    internal static bool IsCurrent(EventEntry entry)
    {
        var last = Read(entry.Revisions).LastOrDefault();
        return last?.Hash is not null
            && entry.RevisedGeneration == last.Generation
            && EventEntryConverter.GetHashForGeneration(entry, new(last.Generation)).Value == last.Hash
            && EventEntryConverter.GetAllGenerationalContent(entry).TryGetValue((int)last.Generation, out var content)
            && SameContent(content, last.Content);
    }

    /// <summary>
    /// Resolves revision authors independently of the original event author.
    /// </summary>
    /// <param name="entry">The current event row.</param>
    /// <param name="identities">The identity storage.</param>
    /// <returns>The known audit history.</returns>
    internal static async Task<IEnumerable<EventRevision>> Restore(EventEntry entry, IIdentityStorage identities)
    {
        if (entry.Type == GlobalEventTypes.Redaction) return [];
        var revisions = new List<EventRevision>();
        foreach (var revision in Read(entry.Revisions))
        {
            var causedBy = await identities.GetFor(revision.CausedBy.Select(_ => new IdentityId(_)));
            revisions.Add(new(new(revision.Generation), new(revision.CorrelationId), revision.Causation, causedBy, revision.Occurred, revision.Content));
        }
        return revisions;
    }

    /// <summary>
    /// Reads the original generation-one content from a recognized envelope.
    /// </summary>
    /// <param name="entry">The current event row.</param>
    /// <returns>The original content, or empty for unknown and redacted events.</returns>
    internal static string Original(EventEntry entry)
    {
        var originals = OriginalGenerations(entry);
        return originals.TryGetValue(1, out var content) ? content : string.Empty;
    }

    /// <summary>
    /// Reads original generational content from the envelope.
    /// </summary>
    /// <param name="entry">The stored row.</param>
    /// <returns>The originals, or empty for unknown and redacted history.</returns>
    internal static IReadOnlyDictionary<int, string> OriginalGenerations(EventEntry entry)
    {
        var envelope = ReadEnvelope(entry);
        if (envelope is null || !envelope.Value.TryGetProperty("content", out var content)) return _noContent;
        return content.EnumerateObject().ToDictionary(_ => int.Parse(_.Name), _ => _.Value.GetRawText());
    }

    /// <summary>
    /// Reads original generation hashes from the envelope.
    /// </summary>
    /// <param name="entry">The stored row.</param>
    /// <returns>The original hashes, or empty for unknown and redacted history.</returns>
    internal static IReadOnlyDictionary<int, EventHash> OriginalHashes(EventEntry entry)
    {
        var envelope = ReadEnvelope(entry);
        if (envelope is null) return _noHashes;
        if (!envelope.Value.TryGetProperty("hashes", out var hashes)) return _noHashes;
        if (hashes.ValueKind != JsonValueKind.Object) return _noHashes;
        return hashes.EnumerateObject().ToDictionary(_ => int.Parse(_.Name), _ => new EventHash(_.Value.GetString()!));
    }

    /// <summary>
    /// Merges original generations over the current generations without changing current event content.
    /// </summary>
    /// <typeparam name="T">The generation value.</typeparam>
    /// <param name="current">The legacy current generations.</param>
    /// <param name="original">The original generations.</param>
    /// <returns>The combined generations, with originals winning collisions.</returns>
    internal static IReadOnlyDictionary<int, T> Merge<T>(IReadOnlyDictionary<int, T> current, IReadOnlyDictionary<int, T> original) =>
        original.Count == 0 ? current : current.Concat(original).GroupBy(_ => _.Key).ToDictionary(_ => _.Key, _ => _.Last().Value);

    static JsonElement? ReadEnvelope(EventEntry entry)
    {
        if (entry.Type == GlobalEventTypes.Redaction) return null;
        if (string.IsNullOrEmpty(entry.OriginalContent)) return null;
        using var envelope = JsonDocument.Parse(entry.OriginalContent);
        var root = envelope.RootElement;
        if (!root.TryGetProperty("format", out var format)) return null;
        if (format.ValueKind != JsonValueKind.Number) return null;
        if (!format.TryGetInt32(out var version)) return null;
        if (version != 1) return null;
        return root.Clone();
    }

    static bool SameContent(string current, string recorded) =>
        System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(current), System.Text.Json.Nodes.JsonNode.Parse(recorded));
}

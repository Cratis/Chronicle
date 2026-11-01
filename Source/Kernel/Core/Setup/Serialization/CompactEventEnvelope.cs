// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries the common event metadata without a schema or a second copy of content.
/// </summary>
internal sealed record CompactEventEnvelope
{
    /// <summary>Gets the complete event context.</summary>
    public required EventContext Context { get; init; }

    /// <summary>Gets the original JSON content.</summary>
    public required string OriginalContent { get; init; }

    /// <summary>Gets the applied revisions.</summary>
    public required IEnumerable<EventRevision> Revisions { get; init; }

    /// <summary>Gets the content stored for each generation.</summary>
    public required IReadOnlyDictionary<int, string> GenerationalContent { get; init; }

    /// <summary>Gets the durable hash for each generation.</summary>
    public required IReadOnlyDictionary<int, EventHash> GenerationalHashes { get; init; }

    /// <summary>Gets the separately stored revision generation.</summary>
    public EventTypeGeneration? RevisedGeneration { get; init; }

    /// <summary>
    /// Captures the complete common envelope without copying content.
    /// </summary>
    /// <param name="value">The source event.</param>
    /// <returns>The transport envelope.</returns>
    internal static CompactEventEnvelope From(AppendedEvent value) => new()
    {
        Context = value.Context,
        OriginalContent = value.OriginalContent,
        Revisions = value.Revisions,
        GenerationalContent = value.GenerationalContent,
        GenerationalHashes = value.GenerationalHashes,
        RevisedGeneration = value.RevisedGeneration
    };

    /// <summary>
    /// Reconstructs an event with the separately decoded content tree.
    /// </summary>
    /// <param name="content">The typed content.</param>
    /// <returns>The complete event.</returns>
    internal AppendedEvent WithContent(System.Dynamic.ExpandoObject content) => new(Context, content)
    {
        OriginalContent = OriginalContent,
        Revisions = Revisions,
        GenerationalContent = GenerationalContent,
        GenerationalHashes = GenerationalHashes,
        RevisedGeneration = RevisedGeneration
    };
}

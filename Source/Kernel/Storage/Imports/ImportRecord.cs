// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents the durable state of an import session.
/// </summary>
/// <param name="Id">The import identifier.</param>
/// <param name="EventSequenceId">The event sequence the import targets.</param>
/// <param name="OnInvalid">What the import does with invalid events.</param>
/// <param name="Created">When the import was begun.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Imports.ImportRecord")]
public record ImportRecord([Id(0)] ImportId Id, [Id(1)] EventSequenceId EventSequenceId, [Id(2)] OnInvalidEvent OnInvalid, [Id(3)] DateTimeOffset Created)
{
    /// <summary>
    /// The most rejections an import keeps; later ones are only counted.
    /// </summary>
    public const int MaxRetainedRejections = 1000;

    /// <summary>
    /// Gets the state of the import.
    /// </summary>
    [Id(4)]
    public ImportState State { get; init; } = ImportState.Open;

    /// <summary>
    /// Gets when the import last saw activity; staging retention counts from here.
    /// </summary>
    [Id(5)]
    public DateTimeOffset LastActivity { get; init; } = Created;

    /// <summary>
    /// Gets the number of chunks the client declared when completing the import.
    /// </summary>
    [Id(6)]
    public int ExpectedChunks { get; init; }

    /// <summary>
    /// Gets the number of events the client declared when completing the import.
    /// </summary>
    [Id(7)]
    public long ExpectedEvents { get; init; }

    /// <summary>
    /// Gets the number of events staged.
    /// </summary>
    [Id(8)]
    public long EventsStaged { get; init; }

    /// <summary>
    /// Gets the number of events committed to the event sequence by this import.
    /// </summary>
    [Id(9)]
    public long EventsCommitted { get; init; }

    /// <summary>
    /// Gets the number of events whose idempotency key was already durable, and so were not appended again.
    /// </summary>
    [Id(10)]
    public long AlreadyAppended { get; init; }

    /// <summary>
    /// Gets the number of rejected events, including those beyond <see cref="MaxRetainedRejections"/>.
    /// </summary>
    [Id(11)]
    public long RejectedCount { get; init; }

    /// <summary>
    /// Gets the retained rejections.
    /// </summary>
    [Id(12)]
    public IEnumerable<ImportRejection> Rejections { get; init; } = [];

    /// <summary>
    /// Gets the index of the last chunk the commit has settled; -1 before the first.
    /// </summary>
    [Id(13)]
    public int LastCommittedChunk { get; init; } = -1;

    /// <summary>
    /// Gets the sequence number the commit started from; nothing the import committed is below it.
    /// </summary>
    [Id(14)]
    public ulong? CommitStartedAt { get; init; }

    /// <summary>
    /// Gets the sequence number from which the log may hold events of the next chunk to commit.
    /// </summary>
    [JsonIgnore]
    public ulong? CommittedFrom => CommittedRanges.Any() ? CommittedRanges.Max(_ => _.Last) + 1 : CommitStartedAt;

    /// <summary>
    /// Gets the sequence number ranges committed so far.
    /// </summary>
    [Id(15)]
    public IEnumerable<ImportCommittedRange> CommittedRanges { get; init; } = [];

    /// <summary>
    /// Gets why the import failed, if it did.
    /// </summary>
    [Id(16)]
    public string? FailureReason { get; init; }

    /// <summary>
    /// Gets the first sequence number the import committed, if any.
    /// </summary>
    [JsonIgnore]
    public ulong? FirstSequenceNumber => CommittedRanges.Any() ? CommittedRanges.Min(_ => _.First) : null;

    /// <summary>
    /// Gets the last sequence number the import committed, if any.
    /// </summary>
    [JsonIgnore]
    public ulong? LastSequenceNumber => CommittedRanges.Any() ? CommittedRanges.Max(_ => _.Last) : null;

    /// <summary>
    /// Adds rejections, keeping at most <see cref="MaxRetainedRejections"/>.
    /// </summary>
    /// <param name="rejections">The rejections to add.</param>
    /// <returns>The updated <see cref="ImportRecord"/>.</returns>
    public ImportRecord WithRejections(IEnumerable<ImportRejection> rejections)
    {
        var added = rejections.ToArray();
        if (added.Length == 0) return this;
        var retained = Rejections.Concat(added).Take(MaxRetainedRejections).ToArray();
        return this with { Rejections = retained, RejectedCount = RejectedCount + added.Length };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the result of an import.
/// </summary>
public record ImportResult
{
    /// <summary>
    /// Gets the <see cref="Imports.ImportId"/>.
    /// </summary>
    public ImportId ImportId { get; init; } = ImportId.NotSet;

    /// <summary>
    /// Gets the <see cref="ImportState"/> the import reached.
    /// </summary>
    public ImportState State { get; init; }

    /// <summary>
    /// Gets how many events were staged.
    /// </summary>
    public long EventsStaged { get; init; }

    /// <summary>
    /// Gets how many events were committed.
    /// </summary>
    public long EventsCommitted { get; init; }

    /// <summary>
    /// Gets how many events were already appended under their idempotency key, and so not appended again.
    /// </summary>
    public long AlreadyAppended { get; init; }

    /// <summary>
    /// Gets the first sequence number the import committed, if it committed any.
    /// </summary>
    public EventSequenceNumber? FirstSequenceNumber { get; init; }

    /// <summary>
    /// Gets the last sequence number the import committed, if it committed any.
    /// </summary>
    public EventSequenceNumber? LastSequenceNumber { get; init; }

    /// <summary>
    /// Gets how many events were rejected, including any beyond the retained <see cref="Rejections"/>.
    /// </summary>
    public long RejectedCount { get; init; }

    /// <summary>
    /// Gets the rejected events, each with its index in the import, its event type and the reasons.
    /// </summary>
    public IEnumerable<ImportRejection> Rejections { get; init; } = [];

    /// <summary>
    /// Gets why the import failed or was abandoned, if it was.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Gets a value indicating whether the import completed.
    /// </summary>
    public bool IsSuccess => State == ImportState.Completed;
}

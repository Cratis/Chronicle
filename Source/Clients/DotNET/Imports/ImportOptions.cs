// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the options for an import.
/// </summary>
public record ImportOptions
{
    /// <summary>
    /// Gets the default <see cref="ImportOptions"/>.
    /// </summary>
    public static readonly ImportOptions Default = new();

    /// <summary>
    /// Gets what the import does when one of its events is invalid. Defaults to <see cref="OnInvalidEvent.Reject"/>: nothing is committed.
    /// </summary>
    public OnInvalidEvent OnInvalid { get; init; } = OnInvalidEvent.Reject;

    /// <summary>
    /// Gets a value indicating whether completing the import waits until it is committed. Defaults to true.
    /// </summary>
    public bool WaitForCommit { get; init; } = true;

    /// <summary>
    /// Gets how many chunks are sent to the kernel at the same time. Defaults to 4.
    /// </summary>
    public int MaxChunksInFlight { get; init; } = 4;

    /// <summary>
    /// Gets the most events the client puts in one chunk; the server's own limit applies when it is lower.
    /// </summary>
    public int? MaxEventsPerChunk { get; init; }

    /// <summary>
    /// Gets the identifier to give the import; a new one is created when not set. Beginning an import with an
    /// identifier that already exists returns that import.
    /// </summary>
    public ImportId? ImportId { get; init; }

    /// <summary>
    /// Gets how many times a chunk is sent again after the connection fails. Resending is safe: the kernel stages a chunk once.
    /// </summary>
    public int MaxChunkAttempts { get; init; } = 5;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Confirmed progress of a kernel reindex job, never a wire contract.
/// </summary>
/// <param name="WroteAnything">Whether index writes were issued.</param>
/// <param name="ProcessedThrough">The last fully processed event, or null when progress is unknown.</param>
[GenerateSerializer]
public sealed record ConstraintIndexRebuildOutcome(
    [property: Id(0)] bool WroteAnything,
    [property: Id(1)] EventSequenceNumber? ProcessedThrough)
{
    /// <summary>
    /// Gets the outcome when no index writes were issued.
    /// </summary>
    public static ConstraintIndexRebuildOutcome NothingWritten { get; } = new(false, null);

    /// <summary>
    /// Gets the conservative outcome when index progress cannot be confirmed.
    /// </summary>
    public static ConstraintIndexRebuildOutcome ProgressUnknown { get; } = new(true, null);
}

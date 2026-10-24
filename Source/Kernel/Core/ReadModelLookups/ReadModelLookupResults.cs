// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModelLookups;

/// <summary>
/// Maps storage outcomes to wire results without exposing an instance on failure.
/// </summary>
internal static class ReadModelLookupResults
{
    /// <summary>Creates a result without an instance.</summary>
    /// <param name="outcome">The lookup outcome.</param>
    /// <param name="message">The reason, when available.</param>
    /// <returns>The empty wire result.</returns>
    internal static ReadModelLookup Empty(ReadModelLookupOutcome outcome, string message = "") => new(outcome, "null", EventSequenceNumber.Unavailable.Value, message);

    /// <summary>Maps a storage outcome to a wire result.</summary>
    /// <param name="outcome">The storage outcome.</param>
    /// <returns>The empty result describing that outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The outcome cannot describe an empty result.</exception>
    internal static ReadModelLookup From(CompositeKeyLookupOutcome outcome) => outcome switch
    {
        CompositeKeyLookupOutcome.NotFound => Empty(ReadModelLookupOutcome.NotFound),
        CompositeKeyLookupOutcome.AmbiguousLegacyKey => Empty(ReadModelLookupOutcome.AmbiguousLegacyKey, "Several stored instances match this key under the legacy id; the next event for this key or a replay stamps it."),
        CompositeKeyLookupOutcome.KeyCollision => Empty(ReadModelLookupOutcome.KeyCollision, "Another key owns the legacy id of this key."),
        CompositeKeyLookupOutcome.NotSupported => Empty(ReadModelLookupOutcome.NotSupported),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };
}

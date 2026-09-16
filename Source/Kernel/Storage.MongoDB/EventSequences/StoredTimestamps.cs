// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Matches append acknowledgments to MongoDB's UTC millisecond timestamp representation.
/// </summary>
internal static class StoredTimestamps
{
    /// <summary>
    /// Resolves the timestamp that MongoDB will retain.
    /// </summary>
    /// <param name="value">The requested timestamp.</param>
    /// <returns>The stored UTC timestamp.</returns>
    internal static DateTimeOffset Normalize(DateTimeOffset value) =>
        DateTimeOffset.FromUnixTimeMilliseconds(value.ToUnixTimeMilliseconds());

    /// <summary>
    /// Resolves a causation entry's stored timestamp without changing its other metadata.
    /// </summary>
    /// <param name="causation">The requested causation entry.</param>
    /// <returns>The entry with its stored timestamp.</returns>
    internal static Causation Normalize(Causation causation) => causation with { Occurred = Normalize(causation.Occurred) };
}

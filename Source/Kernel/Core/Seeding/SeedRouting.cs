// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Normalizes routing from legacy seed entries and requests.
/// </summary>
internal static class SeedRouting
{
    /// <summary>
    /// Normalizes an event source type.
    /// </summary>
    /// <param name="value">The persisted value.</param>
    /// <returns>The type, or the legacy default.</returns>
    internal static EventSourceType SourceType(string? value) => string.IsNullOrEmpty(value) ? EventSourceType.Default : new(value);

    /// <summary>
    /// Normalizes an event stream type.
    /// </summary>
    /// <param name="value">The persisted value.</param>
    /// <returns>The type, or the legacy default.</returns>
    internal static EventStreamType StreamType(string? value) => string.IsNullOrEmpty(value) ? EventStreamType.All : new(value);

    /// <summary>
    /// Normalizes an event stream identifier.
    /// </summary>
    /// <param name="value">The persisted value.</param>
    /// <returns>The identifier, or the legacy default.</returns>
    internal static EventStreamId StreamId(string? value) => string.IsNullOrEmpty(value) ? EventStreamId.Default : new(value);
}

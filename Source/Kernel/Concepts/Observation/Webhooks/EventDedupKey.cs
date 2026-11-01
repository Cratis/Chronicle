// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Identifies an event for one observer independently of delivery batching.
/// </summary>
/// <param name="Value">The canonical event deduplication key.</param>
public record EventDedupKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to an event deduplication key.
    /// </summary>
    /// <param name="value">The key.</param>
    /// <returns>The event deduplication key.</returns>
    public static implicit operator EventDedupKey(string value) => new(value);
}

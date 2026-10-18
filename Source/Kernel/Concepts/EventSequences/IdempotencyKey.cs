// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences;

/// <summary>
/// Identifies an immutable append intent within an event sequence. Must not contain personal data.
/// </summary>
/// <param name="Value">The caller's opaque key.</param>
public record IdempotencyKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The maximum accepted key length.
    /// </summary>
    public const int MaxLength = 512;

    /// <summary>
    /// Gets the durable identity, disjoint from publication sink identities.
    /// </summary>
    public string Identity => $"append-key:{Value}";
}

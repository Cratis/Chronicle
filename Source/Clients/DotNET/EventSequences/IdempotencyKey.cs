// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a caller-chosen key that makes an append idempotent: appending again with the same key never writes the event twice.
/// </summary>
/// <remarks>
/// There is deliberately no implicit conversion from <see cref="string"/>; a key is always created explicitly.
/// Never put personal data in a key - it is stored with the event.
/// </remarks>
public record IdempotencyKey : ConceptAs<string>
{
    /// <summary>
    /// The maximum number of characters in a key.
    /// </summary>
    public const int MaximumLength = 512;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyKey"/> record.
    /// </summary>
    /// <param name="value">The key, 1 to <see cref="MaximumLength"/> characters.</param>
    /// <exception cref="IdempotencyKeyNotValid">The key is empty or longer than <see cref="MaximumLength"/>.</exception>
    public IdempotencyKey(string value)
        : base(IsValid(value) ? value : throw new IdempotencyKeyNotValid())
    {
    }

    /// <summary>
    /// Derives the key for one event of a batch, so a batch keyed once is keyed per event.
    /// </summary>
    /// <param name="index">The zero-based position of the event in the batch.</param>
    /// <returns>A derived key of 64 characters.</returns>
    public IdempotencyKey ForIndex(int index) => new(IdempotencyKeyHashing.Hash(Value, "batch", index.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    static bool IsValid(string? value) => !string.IsNullOrEmpty(value) && value.Length <= MaximumLength;
}

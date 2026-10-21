// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents the compare-and-swap revision of an observer delivery root.
/// </summary>
/// <param name="Value">The revision, bounded by <see cref="long.MaxValue"/>.</param>
public sealed record DeliveryRevision(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// Gets the expected revision for creating a root.
    /// </summary>
    public static readonly DeliveryRevision None = new(0);

    /// <summary>
    /// Advances the revision without wrapping or exceeding the storage range.
    /// </summary>
    /// <returns>The next revision.</returns>
    /// <exception cref="DeliveryRevisionExhausted">The revision is at its maximum.</exception>
    public DeliveryRevision Next()
    {
        _ = DeliveryRanges.Counter(Value, nameof(DeliveryRevision));
        if (Value == long.MaxValue)
        {
            throw new DeliveryRevisionExhausted();
        }

        return new(checked(Value + 1));
    }
}

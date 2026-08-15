// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Concept that represents the stable identity of the delivery of one event to one reactor partition.
/// </summary>
/// <param name="Value">Actual value.</param>
/// <remarks>
/// The value is the same every time the same event is delivered to the same reactor - the retry after a failed
/// partition is recovered, and the re-delivery a replay performs - and different for every genuinely distinct
/// delivery. That makes it the key to record a completed side effect under, so that a redelivery can recognize
/// the work as already done.
/// <para>
/// It identifies the delivery, not the outcome. Chronicle never learns whether the side effect ran, so an
/// identity on its own suppresses nothing - see <see cref="ReactorDelivery"/> for what it does and does not
/// promise.
/// </para>
/// </remarks>
public record DeliveryId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unset <see cref="DeliveryId"/>.
    /// </summary>
    public static readonly DeliveryId NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="DeliveryId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator DeliveryId(string id) => new(id);
}

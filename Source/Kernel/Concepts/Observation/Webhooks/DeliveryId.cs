// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Identifies a logical HTTP delivery independently of its attempts.
/// </summary>
/// <param name="Value">The canonical delivery identifier.</param>
public record DeliveryId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to a delivery identifier.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <returns>The delivery identifier.</returns>
    public static implicit operator DeliveryId(string value) => new(value);
}

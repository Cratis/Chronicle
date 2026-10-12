// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents the persisted delivery root format.
/// </summary>
/// <param name="Value">The positive format number.</param>
public sealed record DeliveryRootFormat(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Gets the current root format.
    /// </summary>
    public static readonly DeliveryRootFormat Current = new(1);
}

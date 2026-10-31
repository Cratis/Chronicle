// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents the activation epoch fencing delivery ownership.
/// </summary>
/// <param name="Value">The epoch, bounded by <see cref="long.MaxValue"/> before storage.</param>
public sealed record DeliveryEpoch(ulong Value) : ConceptAs<ulong>(Value);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a generation isolating delivery rows within an observer.
/// </summary>
/// <param name="Value">The generation, bounded by <see cref="long.MaxValue"/> before storage.</param>
public sealed record DeliveryGeneration(ulong Value) : ConceptAs<ulong>(Value);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// The exception that is thrown when a delivery value exceeds the provider-neutral range; nothing is written.
/// </summary>
/// <param name="field">The invalid field.</param>
public sealed class DeliveryValueOutOfRange(string field) : Exception($"Observer delivery field '{field}' is outside its supported range.");

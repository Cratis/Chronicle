// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// The exception that is thrown when a delivery revision cannot advance within the storage range.
/// </summary>
public sealed class DeliveryRevisionExhausted() : Exception("The observer delivery revision has reached its maximum.");

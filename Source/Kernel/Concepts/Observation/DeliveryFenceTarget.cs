// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Identifies the original client subscriber activation, independently of later subscriptions.
/// </summary>
/// <param name="SubscriberType">The assembly-qualified grain interface name.</param>
/// <param name="SubscriberKey">The original subscriber grain key.</param>
/// <param name="ConnectionId">The original client connection identifier.</param>
/// <param name="SiloAddress">The original silo address.</param>
public record DeliveryFenceTarget(string SubscriberType, string SubscriberKey, string ConnectionId, string SiloAddress);

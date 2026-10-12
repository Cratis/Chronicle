// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Defines the provider-neutral delivery page size policy.
/// </summary>
public static class DeliveryPaging
{
    /// <summary>
    /// Clamps a requested page size to the supported range of one through one thousand.
    /// </summary>
    /// <param name="limit">The requested page size.</param>
    /// <returns>The bounded page size.</returns>
    public static int Clamp(int limit) => Math.Clamp(limit, 1, 1000);
}

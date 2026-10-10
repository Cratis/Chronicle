// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the seeding features supported by the kernel.
/// </summary>
/// <param name="RoutingSupported">Whether seed entries retain their event source and stream routing.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSeeding)]
public record EventSeedingSupport(bool RoutingSupported)
{
    /// <summary>
    /// Gets the seeding features supported by this kernel.
    /// </summary>
    /// <returns>The supported seeding features.</returns>
    internal static Task<EventSeedingSupport> GetSeedingSupport() => Task.FromResult(new EventSeedingSupport(true));
}

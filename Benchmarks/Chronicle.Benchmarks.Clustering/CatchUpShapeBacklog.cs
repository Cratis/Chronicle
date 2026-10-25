// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// The observer's position when the measured catch-up starts.
/// </summary>
/// <remarks>
/// The kernel resubscribes a projection when its client registers again (for example after a reconnect during a long
/// seed), so a projection can handle part of the backlog live before the measurement starts. The benchmark reports
/// that instead of failing, and the measured rate uses the backlog that was actually left.
/// </remarks>
/// <param name="Subscribed">Whether the observer was subscribed again when seeding finished.</param>
/// <param name="LastHandled">The last handled sequence number when seeding finished.</param>
/// <param name="Remaining">The number of sequence numbers left between the last handled event and the tail.</param>
internal sealed record CatchUpShapeBacklog(bool Subscribed, ulong LastHandled, ulong Remaining);

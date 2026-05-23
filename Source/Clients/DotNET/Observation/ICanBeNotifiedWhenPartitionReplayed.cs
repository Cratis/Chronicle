// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a contract for receiving notifications when replay of a specific partition begins and ends.
/// </summary>
public interface ICanBeNotifiedWhenPartitionReplayed
{
    /// <summary>
    /// Called when replay of a partition begins.
    /// </summary>
    /// <param name="partition">The <see cref="Partition"/> being replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplayPartition(Partition partition);

    /// <summary>
    /// Called when replay of a partition ends.
    /// </summary>
    /// <param name="partition">The <see cref="Partition"/> that was replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplayPartition(Partition partition);
}

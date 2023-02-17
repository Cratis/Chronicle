// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the reminder for failed partitions aspect.
/// </summary>
public partial class ObserverSupervisor
{
    /// <inheritdoc/>
    public async Task TryResumePartition(EventSourceId partition)
    {
        if (State.IsDisconnected || !State.IsPartitionFailed(partition) || _failedPartitionSupervisor is null)
        {
            return;
        }

        await _failedPartitionSupervisor.TryRecoveringPartition(partition);
    }
}

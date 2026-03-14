// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ReplayPartitionContract = Cratis.Chronicle.Contracts.Observation.ReplayPartition;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Replays a specific partition of an observer.
/// </summary>
public class ReplayPartitionCommand : ChronicleCommand<PartitionCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, PartitionCommandSettings settings, string format)
    {
        await services.Observers.ReplayPartition(new ReplayPartitionContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId,
            Partition = settings.Partition
        });

        OutputFormatter.WriteMessage(format, $"Replay started for partition '{settings.Partition}' of observer '{settings.ObserverId}'");
        return ExitCodes.Success;
    }
}

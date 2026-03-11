// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Retries a failed partition.
/// </summary>
public class RetryPartitionCommand : ChronicleCommand<PartitionCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, PartitionCommandSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        await services.Observers.RetryPartition(new RetryPartitionContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId,
            Partition = settings.Partition
        });

        OutputFormatter.WriteMessage(format, $"Retry started for partition '{settings.Partition}' of observer '{settings.ObserverId}'");
        return 0;
    }
}

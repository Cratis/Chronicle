// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.Commands.FailedPartitions;

/// <summary>
/// Lists failed partitions.
/// </summary>
public class ListFailedPartitionsCommand : ChronicleCommand<ListFailedPartitionsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ListFailedPartitionsSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId
        });

        var list = failedPartitions.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Id", "ObserverId", "Partition", "Attempts"],
            fp =>
            [
                fp.Id.ToString(),
                fp.ObserverId,
                fp.Partition,
                fp.Attempts.Count().ToString()
            ]);

        return ExitCodes.Success;
    }
}

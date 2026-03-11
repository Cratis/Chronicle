// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Lists read model definitions in an event store.
/// </summary>
public class ListReadModelsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, EventStoreSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var response = await services.ReadModels.GetDefinitions(new GetDefinitionsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        OutputFormatter.Write(
            format,
            response.ReadModels,
            ["Identifier", "Container", "DisplayName", "ObserverType", "Owner", "Source"],
            rm =>
            [
                rm.Type?.Identifier ?? string.Empty,
                rm.ContainerName,
                rm.DisplayName,
                rm.ObserverType.ToString(),
                rm.Owner.ToString(),
                rm.Source.ToString()
            ]);

        return ExitCodes.Success;
    }
}

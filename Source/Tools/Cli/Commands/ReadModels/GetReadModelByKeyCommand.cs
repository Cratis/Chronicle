// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Gets a single read model instance by key.
/// </summary>
public class GetReadModelByKeyCommand : ChronicleCommand<ReadModelKeySettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ReadModelKeySettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var response = await services.ReadModels.GetInstanceByKey(new GetInstanceByKeyRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ReadModelIdentifier = settings.ReadModel,
            EventSequenceId = settings.EventSequenceId,
            ReadModelKey = settings.Key
        });

        if (string.IsNullOrEmpty(response.ReadModel))
        {
            OutputFormatter.WriteError(format, $"No instance found for key '{settings.Key}' in read model '{settings.ReadModel}'");
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                response.ReadModel,
                response.ProjectedEventsCount,
                response.LastHandledEventSequenceNumber
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]ProjectedEvents:[/] {data.ProjectedEventsCount}");
                AnsiConsole.MarkupLine($"[bold]LastHandled#:[/]    {data.LastHandledEventSequenceNumber}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine(data.ReadModel);
            });

        return ExitCodes.Success;
    }
}

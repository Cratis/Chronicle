// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Shows detailed information about a specific observer.
/// </summary>
public class ShowObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ObserverCommandSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var info = await services.Observers.GetObserverInformation(new GetObserverInformationRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        var eventTypes = (info.EventTypes ?? []).Select(et => $"{et.Id}+{et.Generation}").ToList();

        OutputFormatter.WriteObject(
            format,
            new
            {
                info.Id,
                info.EventSequenceId,
                Type = info.Type.ToString(),
                Owner = info.Owner.ToString(),
                RunningState = info.RunningState.ToString(),
                info.NextEventSequenceNumber,
                info.LastHandledEventSequenceNumber,
                info.IsSubscribed,
                EventTypes = eventTypes
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]Observer:[/]     {data.Id.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Sequence:[/]     {data.EventSequenceId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Type:[/]         {data.Type}");
                AnsiConsole.MarkupLine($"[bold]Owner:[/]        {data.Owner}");
                AnsiConsole.MarkupLine($"[bold]State:[/]        {data.RunningState}");
                AnsiConsole.MarkupLine($"[bold]Next#:[/]        {data.NextEventSequenceNumber}");
                AnsiConsole.MarkupLine($"[bold]LastHandled#:[/] {data.LastHandledEventSequenceNumber}");
                AnsiConsole.MarkupLine($"[bold]Subscribed:[/]   {data.IsSubscribed}");
                AnsiConsole.MarkupLine($"[bold]EventTypes:[/]   {string.Join(", ", data.EventTypes)}");
            });

        return ExitCodes.Success;
    }
}

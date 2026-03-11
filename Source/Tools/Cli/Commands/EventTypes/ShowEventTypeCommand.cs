// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.EventTypes;

/// <summary>
/// Shows a specific event type registration including its JSON schema.
/// </summary>
public class ShowEventTypeCommand : ChronicleCommand<ShowEventTypeSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ShowEventTypeSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var (id, generation) = ParseEventType(settings.EventType);

        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var match = registrations.FirstOrDefault(r =>
            string.Equals(r.Type.Id, id, StringComparison.OrdinalIgnoreCase) &&
            r.Type.Generation == generation);

        if (match is null)
        {
            OutputFormatter.WriteError(
                format,
                $"Event type '{settings.EventType}' not found",
                "Use 'cratis event-types list' to see registered event types");
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Type.Id,
                match.Type.Generation,
                match.Type.Tombstone,
                Owner = match.Owner.ToString(),
                Source = match.Source.ToString(),
                match.Schema
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]EventType:[/]  {data.Id.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Generation:[/] {data.Generation}");
                AnsiConsole.MarkupLine($"[bold]Tombstone:[/]  {data.Tombstone}");
                AnsiConsole.MarkupLine($"[bold]Owner:[/]      {data.Owner}");
                AnsiConsole.MarkupLine($"[bold]Source:[/]     {data.Source}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Schema:[/]");
                AnsiConsole.WriteLine(data.Schema);
            });

        return ExitCodes.Success;
    }

    /// <summary>
    /// Parses an event type string into its id and generation components.
    /// Accepts "name" (defaults to generation 1) or "name+generation".
    /// </summary>
    /// <param name="input">The event type string to parse.</param>
    /// <returns>A tuple of id and generation.</returns>
    static (string Id, uint Generation) ParseEventType(string input)
    {
        var parts = input.Split('+');
        var id = parts[0];
        var generation = parts.Length > 1 && uint.TryParse(parts[1], out var gen) ? gen : 1u;
        return (id, generation);
    }
}

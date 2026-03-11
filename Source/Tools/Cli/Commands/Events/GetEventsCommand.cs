// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Gets events from an event sequence.
/// </summary>
public class GetEventsCommand : ChronicleCommand<GetEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, GetEventsSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var request = new GetFromEventSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId,
            FromEventSequenceNumber = settings.From,
            ToEventSequenceNumber = settings.To,
            EventSourceId = settings.EventSourceId
        };

        if (!string.IsNullOrEmpty(settings.EventType))
        {
            var parsed = ParseEventType(settings.EventType);
            request.EventTypes.Add(parsed);
        }

        var response = await services.EventSequences.GetEventsFromEventSequenceNumber(request);

        OutputFormatter.Write(
            format,
            response.Events,
            ["Seq#", "EventType", "EventSourceId", "Occurred"],
            evt =>
            [
                evt.Context.SequenceNumber.ToString(),
                evt.Context.EventType.Id,
                evt.Context.EventSourceId,
                evt.Context.Occurred?.ToString() ?? string.Empty
            ]);

        return ExitCodes.Success;
    }

    /// <summary>
    /// Parses an event type string into a contracts EventType.
    /// Accepts "name" (defaults to generation 1) or "name+generation".
    /// </summary>
    /// <param name="input">The event type string to parse.</param>
    /// <returns>A contracts <see cref="EventType"/> instance.</returns>
    static EventType ParseEventType(string input)
    {
        var parts = input.Split('+');
        return new EventType
        {
            Id = parts[0],
            Generation = parts.Length > 1 && uint.TryParse(parts[1], out var gen) ? gen : 1u
        };
    }
}

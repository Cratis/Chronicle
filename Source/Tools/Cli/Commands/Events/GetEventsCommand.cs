// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            foreach (var parsed in EventTypeParser.ParseEventTypes(settings.EventType))
            {
                request.EventTypes.Add(parsed);
            }
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
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Gets the tail (highest) sequence number in an event sequence. This is not a count of events — gaps may exist.
/// </summary>
public class CountEventsCommand : ChronicleCommand<CountEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, CountEventsSettings settings, string format)
    {
        var request = new GetTailSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId
        };

        if (!string.IsNullOrWhiteSpace(settings.EventType))
        {
            request.EventTypes = EventTypeParser.ParseEventTypes(settings.EventType);
        }

        if (!string.IsNullOrWhiteSpace(settings.EventSourceId))
        {
            request.EventSourceId = settings.EventSourceId;
        }

        var response = await services.EventSequences.GetTailSequenceNumber(request);

        OutputFormatter.WriteObject(format, new { TailSequenceNumber = response.SequenceNumber }, data =>
            Console.WriteLine(data.TailSequenceNumber));

        return ExitCodes.Success;
    }
}

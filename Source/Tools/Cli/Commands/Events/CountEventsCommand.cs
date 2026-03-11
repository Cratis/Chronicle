// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Gets the tail sequence number for an event sequence.
/// </summary>
public class CountEventsCommand : ChronicleCommand<CountEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, CountEventsSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var response = await services.EventSequences.GetTailSequenceNumber(new GetTailSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId
        });

        OutputFormatter.WriteObject(format, new { TailSequenceNumber = response.SequenceNumber }, data =>
            Console.WriteLine(data.TailSequenceNumber));

        return ExitCodes.Success;
    }
}

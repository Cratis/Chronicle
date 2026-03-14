// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Checks whether events exist for a given event source ID.
/// </summary>
public class HasEventsCommand : ChronicleCommand<HasEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, HasEventsSettings settings, string format)
    {
        var response = await services.EventSequences.HasEventsForEventSourceId(new HasEventsForEventSourceIdRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId,
            EventSourceId = settings.EventSourceId
        });

        OutputFormatter.WriteObject(
            format,
            new
            {
                settings.EventSourceId,
                response.HasEvents
            },
            data => Console.WriteLine(data.HasEvents.ToString().ToLowerInvariant()));

        return ExitCodes.Success;
    }
}

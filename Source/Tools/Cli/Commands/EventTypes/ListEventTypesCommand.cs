// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Cli.Commands.EventTypes;

/// <summary>
/// Lists registered event types in an event store.
/// </summary>
public class ListEventTypesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest { EventStore = settings.ResolveEventStore() });
        var list = registrations.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Id", "Generation", "Owner", "Source"],
            reg => [reg.Type.Id, reg.Type.Generation.ToString(), reg.Owner.ToString(), reg.Source.ToString()]);

        return ExitCodes.Success;
    }
}

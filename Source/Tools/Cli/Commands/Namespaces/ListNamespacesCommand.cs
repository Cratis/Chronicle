// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Cli.Commands.Namespaces;

/// <summary>
/// Lists namespaces in an event store.
/// </summary>
public class ListNamespacesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, EventStoreSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);
        var namespaces = await services.Namespaces.GetNamespaces(new GetNamespacesRequest { EventStore = settings.ResolveEventStore() });
        var names = namespaces.ToList();

        OutputFormatter.Write(
            format,
            names,
            ["Namespace"],
            name => [name]);

        return ExitCodes.Success;
    }
}

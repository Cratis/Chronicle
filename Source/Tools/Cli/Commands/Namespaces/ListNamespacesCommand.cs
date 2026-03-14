// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.Namespaces;

/// <summary>
/// Lists namespaces in an event store.
/// </summary>
public class ListNamespacesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
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

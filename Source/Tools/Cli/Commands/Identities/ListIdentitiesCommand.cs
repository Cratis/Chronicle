// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Cli.Commands.Identities;

/// <summary>
/// Lists known identities for an event store.
/// </summary>
public class ListIdentitiesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, EventStoreSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var identities = await services.Identities.GetIdentities(new GetIdentitiesRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace()
        });

        var list = identities.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Subject", "Name", "UserName"],
            id =>
            [
                id.Subject,
                id.Name,
                id.UserName
            ]);

        return 0;
    }
}

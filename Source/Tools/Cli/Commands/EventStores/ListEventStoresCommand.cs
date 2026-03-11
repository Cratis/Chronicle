// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.EventStores;

/// <summary>
/// Lists all event stores.
/// </summary>
public class ListEventStoresCommand : ChronicleCommand<GlobalSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, GlobalSettings settings, string format)
    {
        var eventStores = await client.GetEventStores();
        var names = eventStores.ToList();

        OutputFormatter.Write(
            format,
            names,
            ["Name"],
            name => [name]);

        return 0;
    }
}

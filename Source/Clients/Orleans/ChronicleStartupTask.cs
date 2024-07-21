// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;
extern alias Client;

using Client::Cratis.Chronicle;
using Orleans.Runtime;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleStartupTask"/> class.
/// </remarks>
/// <param name="client"><see cref="IChronicleClient"/> to use.</param>
public class ChronicleStartupTask(IChronicleClient client) : IStartupTask
{
    private readonly IChronicleClient _client = client;

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        var eventStore = _client.GetEventStore("some_event_store");
        await eventStore.DiscoverAll();
        await eventStore.RegisterAll();
    }
}

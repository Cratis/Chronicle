// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// extern alias Server;

namespace Cratis.Chronicle.Orleans.InProcess;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleOrleansClientStartupTask"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> to use for working with events.</param>
public class ChronicleOrleansClientStartupTask(IEventStore eventStore) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await eventStore.DiscoverAll();
        await eventStore.RegisterAll();
    }
}

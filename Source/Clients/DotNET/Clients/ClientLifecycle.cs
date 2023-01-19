// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientLifecycle"/>.
/// </summary>
[Singleton]
public class ClientLifecycle : IClientLifecycle
{
    readonly IInstancesOf<IParticipateInClientLifecycle> _participants;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientLifecycle"/>.
    /// </summary>
    /// <param name="participants">The participants of the client lifecycle.</param>
    public ClientLifecycle(IInstancesOf<IParticipateInClientLifecycle> participants)
    {
        _participants = participants;
        ConnectionId = ConnectionId.New();
    }

    /// <inheritdoc/>
    public async Task Connected()
    {
        IsConnected = true;
        await Parallel.ForEachAsync(_participants, (participant, _) => new ValueTask(participant.ClientConnected()));
    }

    /// <inheritdoc/>
    public async Task Disconnected()
    {
        IsConnected = false;
        await Parallel.ForEachAsync(_participants, (participant, _) => new ValueTask(participant.ClientDisconnected()));
        ConnectionId = ConnectionId.New();
    }
}

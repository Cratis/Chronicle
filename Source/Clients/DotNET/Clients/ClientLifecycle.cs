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

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientLifecycle"/>.
    /// </summary>
    /// <param name="participants">The participants of the client lifecycle.</param>
    public ClientLifecycle(IInstancesOf<IParticipateInClientLifecycle> participants)
    {
        _participants = participants;
    }

    /// <inheritdoc/>
    public Task Connected() => Parallel.ForEachAsync(_participants, (participant, _) => new ValueTask(participant.Connected()));

    /// <inheritdoc/>
    public Task Disconnected() => Parallel.ForEachAsync(_participants, (participant, _) => new ValueTask(participant.Disconnected()));
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionLifecycle"/>.
/// </summary>
[Singleton]
public class ConnectionLifecycle : IConnectionLifecycle
{
    readonly IEnumerable<IParticipateInConnectionLifecycle> _participants;
    readonly ILogger<ConnectionLifecycle> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionLifecycle"/>.
    /// </summary>
    /// <param name="participants">The participants of the client lifecycle.</param>
    /// <param name="logger">Logger for logging.</param>
    public ConnectionLifecycle(
        IEnumerable<IParticipateInConnectionLifecycle> participants,
        ILogger<ConnectionLifecycle> logger)
    {
        _participants = participants;
        _logger = logger;
        ConnectionId = ConnectionId.New();
    }

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; }

    /// <inheritdoc/>
    public async Task Connected()
    {
        IsConnected = true;
        await Parallel.ForEachAsync(_participants, async (participant, _) =>
        {
            try
            {
                await new ValueTask(participant.ClientConnected());
            }
            catch (Exception ex)
            {
                _logger.ParticipantFailedDuringConnected(participant!.GetType().FullName ?? participant!.GetType().Name, ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task Disconnected()
    {
        IsConnected = false;
        await Parallel.ForEachAsync(_participants, async (participant, _) =>
        {
            try
            {
                await new ValueTask(participant.ClientDisconnected());
            }
            catch (Exception ex)
            {
                _logger.ParticipantFailedDuringDisconnected(participant!.GetType().FullName ?? participant!.GetType().Name, ex);
            }
        });
        ConnectionId = ConnectionId.New();
    }
}

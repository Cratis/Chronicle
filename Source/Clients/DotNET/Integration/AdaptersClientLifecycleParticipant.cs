// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents a <see cref="IParticipateInClientLifecycle"/> for handling <see cref="IAdapters"/>.
/// </summary>
public class AdaptersClientLifecycleParticipant : IParticipateInClientLifecycle
{
    readonly IAdapters _adapters;
    readonly ILogger<AdaptersClientLifecycleParticipant> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptersClientLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> to work with.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public AdaptersClientLifecycleParticipant(
        IAdapters adapters,
        ILogger<AdaptersClientLifecycleParticipant> logger)
    {
        _adapters = adapters;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ClientConnected()
    {
        _logger.RegisterAdapters();
        return _adapters.Initialize();
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}

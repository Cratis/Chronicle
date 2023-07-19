// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents a <see cref="IParticipateInConnectionLifecycle"/> for handling <see cref="IAdapters"/>.
/// </summary>
public class AdaptersConnectionLifecycleParticipant : IParticipateInConnectionLifecycle
{
    readonly IAdapters _adapters;
    readonly ILogger<AdaptersConnectionLifecycleParticipant> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptersConnectionLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> to work with.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public AdaptersConnectionLifecycleParticipant(
        IAdapters adapters,
        ILogger<AdaptersConnectionLifecycleParticipant> logger)
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

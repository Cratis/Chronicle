// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents a <see cref="IParticipateInClientLifecycle"/> for handling <see cref="IAdapters"/>.
/// </summary>
public class AdaptersClientLifecycleParticipant : IParticipateInClientLifecycle
{
    readonly IAdapters _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptersClientLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="adapters"><see cref="IAdapters"/> to work with.</param>
    public AdaptersClientLifecycleParticipant(IAdapters adapters) => _adapters = adapters;

    /// <inheritdoc/>
    public Task ClientConnected() => _adapters.Initialize();

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}

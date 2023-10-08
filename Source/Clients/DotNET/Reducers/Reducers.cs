// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
public class Reducers : IReducers
{
    readonly IEventStore _eventStore;
    readonly IClientArtifactsProvider _clientArtifactsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Reducers"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for discovery.</param>
    public Reducers(IEventStore eventStore, IClientArtifactsProvider clientArtifactsProvider)
    {
        _eventStore = eventStore;
        _clientArtifactsProvider = clientArtifactsProvider;
    }

    /// <inheritdoc/>
    public Task Discover() => Task.CompletedTask;
}

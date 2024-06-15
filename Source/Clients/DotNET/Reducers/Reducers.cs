// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducers"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for discovery.</param>
public class Reducers(IEventStore eventStore, IClientArtifactsProvider clientArtifactsProvider) : IReducers
{
    /// <inheritdoc/>
    public Task Discover() => Task.CompletedTask;
}

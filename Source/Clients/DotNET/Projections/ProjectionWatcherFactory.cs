// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionWatcherFactory"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the factory is for.</param>
/// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
public class ProjectionWatcherFactory(IEventStore eventStore, JsonSerializerOptions jsonSerializerOptions) : IProjectionWatcherFactory
{
    /// <inheritdoc/>
    public IProjectionWatcher<TReadModel> Create<TReadModel>(Action stopped) => new ProjectionWatcher<TReadModel>(eventStore, stopped, jsonSerializerOptions);
}

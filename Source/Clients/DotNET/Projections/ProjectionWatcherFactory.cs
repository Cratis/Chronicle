// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionWatcherFactory"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the factory is for.</param>
public class ProjectionWatcherFactory(IEventStore eventStore) : IProjectionWatcherFactory
{
    /// <inheritdoc/>
    public IProjectionWatcher<TModel> Create<TModel>(Action stopped) => new ProjectionWatcher<TModel>(eventStore, () => { });
}

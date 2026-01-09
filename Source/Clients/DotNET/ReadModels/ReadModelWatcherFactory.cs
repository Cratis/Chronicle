// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelWatcherFactory"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the factory is for.</param>
/// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
public class ReadModelWatcherFactory(IEventStore eventStore, JsonSerializerOptions jsonSerializerOptions) : IReadModelWatcherFactory
{
    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> Create<TReadModel>(Action stopped) => new ReadModelWatcher<TReadModel>(eventStore, stopped, jsonSerializerOptions);
}

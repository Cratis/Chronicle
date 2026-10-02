// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSources;
using Cratis.Chronicle.Storage.EventSources;

namespace Cratis.Chronicle.Storage.InMemory.EventSources;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSourcesStorage"/>.
/// </summary>
public sealed class EventSourcesStorage : IEventSourcesStorage, IDisposable
{
    readonly ConcurrentDictionary<EventSourceName, EventSourceDefinition> _definitions = new();
    readonly ReplaySubject<IEnumerable<EventSourceDefinition>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourcesStorage"/> class.
    /// </summary>
    public EventSourcesStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public Task<IEnumerable<EventSourceDefinition>> GetAll() => Task.FromResult<IEnumerable<EventSourceDefinition>>(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventSourceDefinition>> ObserveAll() => _allSubject;

    /// <inheritdoc/>
    public Task<EventSourceDefinition?> Find(EventSourceName name) =>
        Task.FromResult(_definitions.TryGetValue(name, out var definition) ? definition : null);

    /// <inheritdoc/>
    public Task Save(EventSourceDefinition definition)
    {
        _definitions[definition.Name] = definition with { Streams = [.. definition.Streams] };
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

    EventSourceDefinition[] Snapshot() => [.. _definitions.Values];
}

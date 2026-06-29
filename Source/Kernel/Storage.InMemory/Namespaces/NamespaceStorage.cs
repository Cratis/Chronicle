// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces;

/// <summary>
/// Represents an in-memory implementation of <see cref="INamespaceStorage"/>.
/// </summary>
public sealed class NamespaceStorage : INamespaceStorage
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, NamespaceState> _namespaces = new();
    readonly ReplaySubject<IEnumerable<NamespaceState>> _subject = new(1);

    /// <inheritdoc/>
    public Task Ensure(EventStoreNamespaceName name)
    {
        if (!_namespaces.ContainsKey(name))
        {
            return Create(name, DateTimeOffset.UtcNow);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Create(EventStoreNamespaceName name, DateTimeOffset created)
    {
        _namespaces[name] = new NamespaceState(name, created);
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(EventStoreNamespaceName name)
    {
        _namespaces.TryRemove(name, out _);
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<NamespaceState>> GetAll() =>
        Task.FromResult<IEnumerable<NamespaceState>>([.. _namespaces.Values]);

    /// <inheritdoc/>
    public ISubject<IEnumerable<NamespaceState>> ObserveAll()
    {
        Publish();
        return _subject;
    }

    void Publish() => _subject.OnNext([.. _namespaces.Values]);
}

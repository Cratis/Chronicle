// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.SequenceQueries;

/// <summary>
/// Represents an in-memory implementation of <see cref="ISequenceQueryStorage"/>.
/// </summary>
public class SequenceQueryStorage : ISequenceQueryStorage, IDisposable
{
    readonly ConcurrentDictionary<string, SequenceQueryDefinition> _definitions = new();
    readonly ConcurrentDictionary<string, ReplaySubject<IEnumerable<SequenceQueryDefinition>>> _subjects = new();

    /// <inheritdoc/>
    public Task<IEnumerable<SequenceQueryDefinition>> GetAllFor(SequenceQueryOwner owner) =>
        Task.FromResult(VisibleTo(owner));

    /// <inheritdoc/>
    public ISubject<IEnumerable<SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner)
    {
        var subject = _subjects.GetOrAdd(owner.Value, _ => new ReplaySubject<IEnumerable<SequenceQueryDefinition>>(1));
        subject.OnNext(VisibleTo(owner));

        return subject;
    }

    /// <inheritdoc/>
    public Task Save(SequenceQueryDefinition definition)
    {
        _definitions[definition.Id.Value] = definition;
        NotifyChange();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(SequenceQueryId id)
    {
        _definitions.TryRemove(id.Value, out _);
        NotifyChange();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var subject in _subjects.Values)
        {
            subject.Dispose();
        }

        _subjects.Clear();
        GC.SuppressFinalize(this);
    }

    IEnumerable<SequenceQueryDefinition> VisibleTo(SequenceQueryOwner owner) =>
    [
        .. _definitions.Values
            .Where(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == owner)
            .OrderBy(_ => _.Name.Value)
    ];

    void NotifyChange()
    {
        foreach (var (owner, subject) in _subjects)
        {
            subject.OnNext(VisibleTo(owner));
        }
    }
}

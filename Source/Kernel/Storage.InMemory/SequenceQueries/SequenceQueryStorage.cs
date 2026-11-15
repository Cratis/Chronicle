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
    readonly ConcurrentDictionary<string, SequenceQueryFolderDefinition> _folders = new();
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
    public Task<SequenceQueryDefinition?> Get(SequenceQueryId id) =>
        Task.FromResult(_definitions.TryGetValue(id.Value, out var definition) ? definition : null);

    /// <inheritdoc/>
    public Task<SequenceQueryFolderDefinition?> GetFolder(SequenceQueryFolderId id) =>
        Task.FromResult(_folders.TryGetValue(id.Value, out var folder) ? folder : null);

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> Insert(SequenceQueryDefinition definition)
    {
        var outcome = ConditionalWrite.Insert(_definitions, definition.Id.Value, definition);
        if (outcome == SequenceQueryWriteOutcome.Applied) NotifyChange();

        return Task.FromResult(outcome);
    }

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> Update(SequenceQueryDefinition definition, SequenceQueryOwner expectedOwner)
    {
        var outcome = ConditionalWrite.Update(_definitions, definition.Id.Value, expectedOwner, _ => _.Owner, definition with { Owner = expectedOwner });
        if (outcome == SequenceQueryWriteOutcome.Applied) NotifyChange();

        return Task.FromResult(outcome);
    }

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> Delete(SequenceQueryId id, SequenceQueryOwner expectedOwner)
    {
        var outcome = ConditionalWrite.Delete(_definitions, id.Value, expectedOwner, _ => _.Owner);
        if (outcome == SequenceQueryWriteOutcome.Applied) NotifyChange();

        return Task.FromResult(outcome);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SequenceQueryFolderDefinition>> GetAllFoldersFor(SequenceQueryOwner owner) =>
        Task.FromResult<IEnumerable<SequenceQueryFolderDefinition>>(
        [
            .. _folders.Values
                .Where(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == owner)
                .OrderBy(_ => _.Path.Value)
        ]);

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> InsertFolder(SequenceQueryFolderDefinition definition) =>
        Task.FromResult(ConditionalWrite.Insert(_folders, definition.Id.Value, definition));

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> UpdateFolder(SequenceQueryFolderDefinition definition, SequenceQueryOwner expectedOwner) =>
        Task.FromResult(ConditionalWrite.Update(_folders, definition.Id.Value, expectedOwner, _ => _.Owner, definition with { Owner = expectedOwner }));

    /// <inheritdoc/>
    public Task<SequenceQueryWriteOutcome> DeleteFolder(SequenceQueryFolderId id, SequenceQueryOwner expectedOwner) =>
        Task.FromResult(ConditionalWrite.Delete(_folders, id.Value, expectedOwner, _ => _.Owner));

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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage.Captures;

namespace Cratis.Chronicle.Storage.InMemory.Captures;

/// <summary>
/// Represents an in-memory implementation of <see cref="ICapturesStorage"/>.
/// </summary>
public sealed class CapturesStorage : ICapturesStorage, IDisposable
{
    readonly ConcurrentDictionary<CaptureId, Capture> _captures = new();
    readonly ReplaySubject<IEnumerable<Capture>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CapturesStorage"/> class.
    /// </summary>
    public CapturesStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public Task<IEnumerable<Capture>> GetAll() => Task.FromResult<IEnumerable<Capture>>(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<Capture>> ObserveAll() => _allSubject;

    /// <inheritdoc/>
    public Task<bool> Has(CaptureId id) =>
        Task.FromResult(_captures.ContainsKey(id));

    /// <inheritdoc/>
    public Task<Capture> Get(CaptureId id) =>
        Task.FromResult(_captures[id]);

    /// <inheritdoc/>
    public Task Delete(CaptureId id)
    {
        _captures.TryRemove(id, out _);
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(Capture capture)
    {
        _captures[capture.Id] = capture;
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

    Capture[] Snapshot() => _captures.Values.ToArray();
}

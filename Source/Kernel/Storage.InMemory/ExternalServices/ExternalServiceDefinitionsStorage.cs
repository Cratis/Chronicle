// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage.ExternalServices;

namespace Cratis.Chronicle.Storage.InMemory.ExternalServices;

/// <summary>
/// Represents an in-memory implementation of <see cref="IExternalServiceDefinitionsStorage"/>.
/// </summary>
public sealed class ExternalServiceDefinitionsStorage : IExternalServiceDefinitionsStorage, IDisposable
{
    readonly ConcurrentDictionary<ExternalServiceId, ExternalServiceDefinition> _definitions = new();
    readonly ReplaySubject<IEnumerable<ExternalServiceDefinition>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceDefinitionsStorage"/> class.
    /// </summary>
    public ExternalServiceDefinitionsStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public Task<IEnumerable<ExternalServiceDefinition>> GetAll() => Task.FromResult<IEnumerable<ExternalServiceDefinition>>(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<ExternalServiceDefinition>> ObserveAll() => _allSubject;

    /// <inheritdoc/>
    public Task<bool> Has(ExternalServiceId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ExternalServiceDefinition> Get(ExternalServiceId id) =>
        Task.FromResult(_definitions[id]);

    /// <inheritdoc/>
    public Task Delete(ExternalServiceId id)
    {
        _definitions.TryRemove(id, out _);
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ExternalServiceDefinition definition)
    {
        _definitions[definition.Id] = definition;
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

    ExternalServiceDefinition[] Snapshot() => _definitions.Values.ToArray();
}

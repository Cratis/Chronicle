// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a decorator around <see cref="IReadModels"/> for testing that intercepts
/// <c>GetInstanceById</c> to return pre-seeded read model instances when available,
/// and delegates all other operations to the real inner implementation.
/// </summary>
/// <param name="inner">The real <see cref="IReadModels"/> implementation to delegate to.</param>
public class ReadModelsForTesting(IReadModels inner) : IReadModels
{
    readonly Dictionary<(string Identifier, string Key), object> _instances = [];

    /// <inheritdoc/>
    public Task Register() => inner.Register();

    /// <inheritdoc/>
    public Task Register<TReadModel>() => inner.Register<TReadModel>();

    /// <inheritdoc/>
    public Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        var identifier = typeof(TReadModel).GetReadModelIdentifier();
        if (_instances.TryGetValue((identifier, key.Value), out var instance))
        {
            return Task.FromResult((TReadModel)instance);
        }

        return inner.GetInstanceById<TReadModel>(key, sessionId);
    }

    /// <inheritdoc/>
    public Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        var identifier = readModelType.GetReadModelIdentifier();
        if (_instances.TryGetValue((identifier, key.Value), out var instance))
        {
            return Task.FromResult(instance);
        }

        return inner.GetInstanceById(readModelType, key, sessionId);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null) =>
        inner.GetInstances<TReadModel>(eventCount);

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey) =>
        inner.GetSnapshotsById<TReadModel>(readModelKey);

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>() =>
        inner.Watch<TReadModel>();

    /// <inheritdoc/>
    public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) =>
        inner.DehydrateSession(sessionId, readModelType, readModelKey);

    /// <inheritdoc/>
    public Task<TReadModel> Release<TReadModel>(TReadModel instance) =>
        inner.Release(instance);

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) =>
        inner.Release(instances);

    /// <summary>
    /// Registers a pre-seeded read model instance so that subsequent <c>GetInstanceById</c> calls
    /// return it directly without hitting the server.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The event source identifier to associate with the read model.</param>
    /// <param name="instance">The read model instance to pre-seed.</param>
    internal void RegisterInstance<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class
    {
        var identifier = typeof(TReadModel).GetReadModelIdentifier();
        _instances[(identifier, eventSourceId.Value)] = instance;
    }
}

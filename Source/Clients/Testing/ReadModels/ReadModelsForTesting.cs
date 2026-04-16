// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/> for testing scenarios.
/// </summary>
/// <remarks>
/// <para>
/// Provides a registry-based implementation that returns pre-registered read model instances
/// for <see cref="GetInstanceById{TReadModel}"/> calls. Use the <c>ReadModel()</c> builder method
/// on <see cref="ReadModelSourceGivenBuilder{TReadModel}"/> to seed instances before exercising
/// production code that depends on <see cref="IReadModels"/>.
/// </para>
/// <para>
/// Methods other than <see cref="GetInstanceById{TReadModel}"/> and <see cref="GetInstanceById"/>
/// throw <see cref="NotSupportedException"/> since they require a live server connection.
/// </para>
/// </remarks>
public class ReadModelsForTesting : IReadModels
{
    readonly Dictionary<(Type ReadModelType, ReadModelKey Key), object> _instances = [];

    /// <summary>
    /// Registers a read model instance for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to register the read model for.</param>
    /// <param name="instance">The read model instance to register.</param>
    public void Register<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class =>
        _instances[(typeof(TReadModel), (ReadModelKey)eventSourceId)] = instance;

    /// <inheritdoc/>
    public Task Register() => throw new NotSupportedException("Register is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task Register<TReadModel>() => throw new NotSupportedException("Register is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        if (_instances.TryGetValue((typeof(TReadModel), key), out var instance))
        {
            return Task.FromResult((TReadModel)instance);
        }

        throw new ReadModelNotRegistered(typeof(TReadModel), key);
    }

    /// <inheritdoc/>
    public Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        if (_instances.TryGetValue((readModelType, key), out var instance))
        {
            return Task.FromResult(instance);
        }

        throw new ReadModelNotRegistered(readModelType, key);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null) =>
        throw new NotSupportedException("GetInstances is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey) =>
        throw new NotSupportedException("GetSnapshotsById is not supported in test scenarios.");

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>() =>
        throw new NotSupportedException("Watch is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) =>
        throw new NotSupportedException("DehydrateSession is not supported in test scenarios.");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_IReadModels.given;

/// <summary>
/// Deliberately implements only the members that predate collection observation.
/// </summary>
/// <param name="inner">The implementation of the preexisting members.</param>
public class LegacyReadModels(IReadModels inner) : IReadModels
{
    public IMaterializedReadModels Materialized => inner.Materialized;

    public Task Register() => inner.Register();

    public Task Register<TReadModel>() => inner.Register<TReadModel>();

    public Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null) => inner.GetInstanceById<TReadModel>(key, sessionId);

    public Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null) => inner.GetInstanceById(readModelType, key, sessionId);

    public Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null) => inner.GetInstances<TReadModel>(eventCount);

    public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey) => inner.GetSnapshotsById<TReadModel>(readModelKey);

    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>() => inner.Watch<TReadModel>();

    public IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>() => inner.GetWatcherFor<TReadModel>();

    public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) => inner.DehydrateSession(sessionId, readModelType, readModelKey);

    public Task<TReadModel> Release<TReadModel>(TReadModel instance) => inner.Release(instance);

    public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) => inner.Release(instances);
}

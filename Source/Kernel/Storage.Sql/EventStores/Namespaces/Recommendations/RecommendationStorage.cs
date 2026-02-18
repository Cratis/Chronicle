// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class RecommendationStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IRecommendationStorage, IDisposable
{
    readonly RecommendationConverter _converter = new();
    readonly Subject<IEnumerable<RecommendationState>> _recommendationsSubject = new();

    /// <inheritdoc/>
    public async Task<RecommendationState?> Get(RecommendationId recommendationId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = await scope.DbContext.Recommendations.FirstOrDefaultAsync(r => r.Id == recommendationId.Value);
        return entity is not null ? _converter.ToRecommendationState(entity) : null;
    }

    /// <inheritdoc/>
    public async Task Save(RecommendationId recommendationId, RecommendationState recommendationState)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = _converter.ToEntity(recommendationId, recommendationState);
        await scope.DbContext.Recommendations.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
        _recommendationsSubject.OnNext(await GetAllInternal());
    }

    /// <inheritdoc/>
    public async Task Remove(RecommendationId recommendationId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entity = await scope.DbContext.Recommendations.FirstOrDefaultAsync(r => r.Id == recommendationId.Value);
        if (entity is not null)
        {
            scope.DbContext.Recommendations.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
            _recommendationsSubject.OnNext(await GetAllInternal());
        }
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<RecommendationState>> GetAll()
    {
        return (await GetAllInternal()).ToImmutableList();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<RecommendationState>> ObserveRecommendations()
    {
        return _recommendationsSubject;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _recommendationsSubject?.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task<IEnumerable<RecommendationState>> GetAllInternal()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entities = await scope.DbContext.Recommendations.ToListAsync();
        return entities.Select(_converter.ToRecommendationState);
    }
}

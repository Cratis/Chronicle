// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Storage.InMemory.Recommendations;

/// <summary>
/// Represents an in-memory implementation of <see cref="IRecommendationStorage"/>.
/// </summary>
public sealed class RecommendationStorage : IRecommendationStorage, IDisposable
{
    readonly ConcurrentDictionary<RecommendationId, RecommendationState> _recommendations = new();
    readonly ReplaySubject<IEnumerable<RecommendationState>> _subject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationStorage"/> class.
    /// </summary>
    public RecommendationStorage() => _subject.OnNext(Snapshot());

    /// <inheritdoc/>
    public Task<RecommendationState?> Get(RecommendationId recommendationId) =>
        Task.FromResult(_recommendations.TryGetValue(recommendationId, out var state) ? state : null);

    /// <inheritdoc/>
    public Task Save(RecommendationId recommendationId, RecommendationState recommendationState)
    {
        _recommendations[recommendationId] = recommendationState;
        _subject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(RecommendationId recommendationId)
    {
        _recommendations.TryRemove(recommendationId, out _);
        _subject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IImmutableList<RecommendationState>> GetAll() =>
        Task.FromResult<IImmutableList<RecommendationState>>(_recommendations.Values.ToImmutableList());

    /// <inheritdoc/>
    public ISubject<IEnumerable<RecommendationState>> ObserveRecommendations() => _subject;

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    RecommendationState[] Snapshot() => _recommendations.Values.ToArray();
}

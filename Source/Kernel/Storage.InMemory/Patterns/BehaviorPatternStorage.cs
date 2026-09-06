// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns;

/// <summary>
/// Represents an in-memory implementation of <see cref="IBehaviorPatternStorage"/>.
/// </summary>
public class BehaviorPatternStorage : IBehaviorPatternStorage
{
    readonly ConcurrentDictionary<PatternGroupingKey, ConcurrentDictionary<FacetSetKey, BehaviorPattern>> _patterns = new();

    /// <inheritdoc/>
    public Task Save(IEnumerable<BehaviorPattern> patterns)
    {
        foreach (var pattern in patterns)
        {
            ForScope(pattern.GroupingKey)[pattern.Facets.Key] = pattern;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BehaviorPattern>> GetForScope(PatternGroupingKey groupingKey) =>
        Task.FromResult<IEnumerable<BehaviorPattern>>(
            _patterns.TryGetValue(groupingKey, out var forScope) ? [.. forScope.Values] : []);

    /// <inheritdoc/>
    public Task<IEnumerable<BehaviorPattern>> GetMatching(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> candidates)
    {
        if (!_patterns.TryGetValue(groupingKey, out var forScope))
        {
            return Task.FromResult<IEnumerable<BehaviorPattern>>([]);
        }

        var matching = candidates
            .Distinct()
            .Where(forScope.ContainsKey)
            .Select(key => forScope[key])
            .ToArray();

        return Task.FromResult<IEnumerable<BehaviorPattern>>(matching);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<PatternGroupingKey>> GetScopes() =>
        Task.FromResult<IEnumerable<PatternGroupingKey>>([.. _patterns.Keys]);

    /// <inheritdoc/>
    public Task RemoveAllExcept(PatternGroupingKey groupingKey, IEnumerable<FacetSetKey> surviving)
    {
        if (!_patterns.TryGetValue(groupingKey, out var forScope))
        {
            return Task.CompletedTask;
        }

        var keep = surviving.ToHashSet();
        foreach (var key in forScope.Keys.Where(key => !keep.Contains(key)))
        {
            forScope.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    ConcurrentDictionary<FacetSetKey, BehaviorPattern> ForScope(PatternGroupingKey groupingKey) =>
        _patterns.GetOrAdd(groupingKey, _ => new ConcurrentDictionary<FacetSetKey, BehaviorPattern>());
}

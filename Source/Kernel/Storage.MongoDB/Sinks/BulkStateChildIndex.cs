// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// A lazy, per-path index of the values held by the documents in a bulk state cache.
/// </summary>
/// <remarks>
/// Cached documents are lent out by reference and changed in place, so the index never trusts a document it has
/// not been told about: whoever inserts, replaces or lends a document calls <see cref="MarkDirty"/>, and the
/// next <see cref="Find"/> re-indexes it. Every candidate is then confirmed against the current document, so a
/// stale bucket entry can never produce an answer the scan over the cache would not.
/// </remarks>
sealed class BulkStateChildIndex
{
    readonly object _lock = new();
    readonly ConcurrentDictionary<string, byte> _dirty = new();
    readonly Dictionary<string, PathIndex> _paths = [];

    /// <summary>
    /// Marks a cached document as possibly changed.
    /// </summary>
    /// <param name="cacheKey">The cache key of the document.</param>
    /// <remarks>
    /// A document that is already marked is left alone: the lock-free read keeps the hot path (every cached
    /// lookup and write) off the dictionary's bucket locks, and only the first mark after a re-index writes.
    /// </remarks>
    public void MarkDirty(string cacheKey)
    {
        if (!_dirty.ContainsKey(cacheKey))
        {
            _dirty.TryAdd(cacheKey, 0);
        }
    }

    /// <summary>
    /// Forgets a document that left the cache.
    /// </summary>
    /// <param name="cacheKey">The cache key of the document.</param>
    public void Remove(string cacheKey)
    {
        _dirty.TryRemove(cacheKey, out _);
        lock (_lock)
        {
            foreach (var path in _paths.Values)
            {
                path.Remove(cacheKey);
            }
        }
    }

    /// <summary>
    /// Forgets everything.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _dirty.Clear();
            _paths.Clear();
        }
    }

    /// <summary>
    /// Finds the cache keys of the documents holding the value at the path.
    /// </summary>
    /// <param name="path">The property path.</param>
    /// <param name="value">The value to look for.</param>
    /// <param name="cache">The cache the index describes.</param>
    /// <returns>The matching cache keys, in the enumeration order of the cache.</returns>
    public IReadOnlyList<string> Find(PropertyPath path, object value, ConcurrentDictionary<string, ExpandoObject> cache)
    {
        lock (_lock)
        {
            Reindex(cache);
            var pathIndex = GetOrBuild(path, cache);
            var candidates = pathIndex.Candidates(value)
                .Where(cacheKey => cache.TryGetValue(cacheKey, out var document) &&
                    BulkStateValueMatch.TryFindValueInDocument(document, pathIndex.Segments, 0, value))
                .ToHashSet();

            return candidates.Count < 2
                ? [.. candidates]
                : [.. cache.Keys.Where(candidates.Contains)];
        }
    }

    void Reindex(ConcurrentDictionary<string, ExpandoObject> cache)
    {
        foreach (var cacheKey in _dirty.Keys)
        {
            _dirty.TryRemove(cacheKey, out _);
            var found = cache.TryGetValue(cacheKey, out var document);
            foreach (var path in _paths.Values)
            {
                if (found)
                {
                    path.Index(cacheKey, document!);
                }
                else
                {
                    path.Remove(cacheKey);
                }
            }
        }
    }

    PathIndex GetOrBuild(PropertyPath path, ConcurrentDictionary<string, ExpandoObject> cache)
    {
        if (_paths.TryGetValue(path.Path, out var existing))
        {
            return existing;
        }

        var built = new PathIndex(path.Segments.ToArray());
        foreach (var (cacheKey, document) in cache)
        {
            built.Index(cacheKey, document);
        }

        _paths[path.Path] = built;
        return built;
    }

    sealed class PathIndex(IPropertyPathSegment[] segments)
    {
        readonly Dictionary<string, HashSet<string>> _buckets = [];
        readonly Dictionary<string, string[]> _bucketsByCacheKey = [];

        public IPropertyPathSegment[] Segments { get; } = segments;

        public void Index(string cacheKey, ExpandoObject document)
        {
            Remove(cacheKey);
            var keys = BulkStateValueMatch.ExtractLeafValues(document, Segments)
                .SelectMany(BulkStateValueMatch.BucketKeys)
                .Distinct()
                .ToArray();
            if (keys.Length == 0)
            {
                return;
            }

            _bucketsByCacheKey[cacheKey] = keys;
            foreach (var key in keys)
            {
                if (!_buckets.TryGetValue(key, out var members))
                {
                    _buckets[key] = members = [];
                }

                members.Add(cacheKey);
            }
        }

        public void Remove(string cacheKey)
        {
            if (!_bucketsByCacheKey.Remove(cacheKey, out var keys))
            {
                return;
            }

            foreach (var key in keys)
            {
                if (_buckets.TryGetValue(key, out var members) && members.Remove(cacheKey) && members.Count == 0)
                {
                    _buckets.Remove(key);
                }
            }
        }

        public IEnumerable<string> Candidates(object value) =>
            BulkStateValueMatch.BucketKeys(value)
                .Where(_buckets.ContainsKey)
                .SelectMany(key => _buckets[key])
                .Distinct();
    }
}

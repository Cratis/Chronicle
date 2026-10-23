// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Lazily indexes child values for paths queried during a bulk window.
/// </summary>
/// <remarks>
/// Duplicate values may belong to several roots; as with the cache scan, no ordering is promised.
/// Reads do not take a global lock. Lazy initialization serializes the first build of each path with
/// updates to that path. Cache writes must precede Update/Remove, just as they precede key publication.
/// </remarks>
internal sealed class BulkStateChildIndex
{
    readonly ConcurrentDictionary<string, Lazy<PathIndex>> _paths = new(StringComparer.Ordinal);

    /// <summary>
    /// Finds candidate cache keys without scanning documents after the first lookup for a path.
    /// </summary>
    /// <param name="path">The child property path.</param>
    /// <param name="value">The child value to match.</param>
    /// <param name="states">The current cached documents.</param>
    /// <returns>The matching cache keys, without a guaranteed order.</returns>
    internal IEnumerable<string> Find(PropertyPath path, object value, ConcurrentDictionary<string, ExpandoObject> states) =>
        _paths.GetOrAdd(path.Path, static (_, args) => new Lazy<PathIndex>(() => Build(args.Path, args.States)), (Path: path, States: states)).Value.Find(value);

    /// <summary>
    /// Replaces the indexed values for a cached document across all materialized paths.
    /// </summary>
    /// <param name="cacheKey">The document cache key.</param>
    /// <param name="document">The current cached state.</param>
    internal void Update(string cacheKey, ExpandoObject document)
    {
        foreach (var path in _paths.Values)
        {
            path.Value.Update(cacheKey, document);
        }
    }

    /// <summary>
    /// Removes a document from every materialized path.
    /// </summary>
    /// <param name="cacheKey">The document cache key.</param>
    internal void Remove(string cacheKey)
    {
        foreach (var path in _paths.Values)
        {
            path.Value.Remove(cacheKey);
        }
    }

    /// <summary>
    /// Discards all materialized paths when the bulk cache is cleared.
    /// </summary>
    internal void Clear() => _paths.Clear();

    static PathIndex Build(PropertyPath path, ConcurrentDictionary<string, ExpandoObject> states)
    {
        var index = new PathIndex(path.Segments.ToArray());
        foreach (var (cacheKey, document) in states)
        {
            index.Update(cacheKey, document);
        }

        return index;
    }

    static IEnumerable<object> ValuesInDocument(ExpandoObject document, IPropertyPathSegment[] segments, int offset)
    {
        if (offset >= segments.Length ||
            !((IDictionary<string, object?>)document).TryGetValue(segments[offset].Value, out var value) || value is null)
        {
            return [];
        }

        return offset == segments.Length - 1 ? [value] : ValuesInChildren(value, segments, offset + 1);
    }

    static IEnumerable<object> ValuesInChildren(object value, IPropertyPathSegment[] segments, int offset) =>
        value switch
        {
            IEnumerable<object> collection => collection.OfType<ExpandoObject>().SelectMany(item => ValuesInDocument(item, segments, offset)),
            ExpandoObject document => ValuesInDocument(document, segments, offset),
            _ => []
        };

    static string BucketKey(object value)
    {
        // A null ToString and an empty ToString share a bucket, but the original equality rule
        // confirms candidates. Null property values are not indexed.
        return value.ToString() ?? string.Empty;
    }

    static IEnumerable<(ValueBucket Bucket, object Value)> BucketsForValue(object value)
    {
        yield return (new(value, false), value);
        yield return (new(BucketKey(value), true), value);
    }

    readonly record struct ValueBucket(object Value, bool IsText);

    sealed class PathIndex(IPropertyPathSegment[] segments)
    {
        readonly object _writeLock = new();
        readonly ConcurrentDictionary<ValueBucket, ConcurrentDictionary<string, object[]>> _buckets = new();
        readonly Dictionary<string, ValueBucket[]> _bucketsByCacheKey = new(StringComparer.Ordinal);

        internal IEnumerable<string> Find(object target) =>
            Candidates(new(target, false), target).Concat(Candidates(new(BucketKey(target), true), target)).Distinct();

        internal void Update(string cacheKey, ExpandoObject document)
        {
            var groups = ValuesInDocument(document, segments, 0).SelectMany(BucketsForValue).GroupBy(item => item.Bucket).ToArray();
            lock (_writeLock)
            {
                RemoveEntries(cacheKey);
                foreach (var group in groups)
                {
                    _buckets.GetOrAdd(group.Key, _ => new(StringComparer.Ordinal))[cacheKey] = group.Select(item => item.Value).ToArray();
                }

                _bucketsByCacheKey[cacheKey] = groups.Select(group => group.Key).ToArray();
            }
        }

        internal void Remove(string cacheKey)
        {
            lock (_writeLock)
            {
                RemoveEntries(cacheKey);
            }
        }

        IEnumerable<string> Candidates(ValueBucket key, object target) =>
            _buckets.TryGetValue(key, out var bucket)
                ? bucket.Where(pair => pair.Value.Any(value => Sink.ValuesAreEqual(value, target))).Select(pair => pair.Key)
                : [];

        void RemoveEntries(string cacheKey)
        {
            if (!_bucketsByCacheKey.Remove(cacheKey, out var bucketKeys))
            {
                return;
            }

            // Remember extracted values rather than rereading the old document: projection changes can
            // mutate the same ExpandoObject before it is cached again.
            foreach (var bucketKey in bucketKeys)
            {
                var bucket = _buckets[bucketKey];
                bucket.TryRemove(cacheKey, out _);
                if (bucket.IsEmpty)
                {
                    _buckets.TryRemove(bucketKey, out _);
                }
            }
        }
    }
}

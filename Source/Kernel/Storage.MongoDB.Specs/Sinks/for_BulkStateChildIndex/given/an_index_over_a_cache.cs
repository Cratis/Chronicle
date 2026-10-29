// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

public class an_index_over_a_cache : Specification
{
    protected ConcurrentDictionary<string, ExpandoObject> cache;
    internal BulkStateChildIndex index;

    void Establish()
    {
        cache = new();
        index = new();
    }

    protected static ExpandoObject Doc(params (string Name, object? Value)[] properties)
    {
        var document = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)document;
        foreach (var (name, value) in properties)
        {
            dictionary[name] = value;
        }

        return document;
    }

    protected static List<object> Items(params ExpandoObject[] items) => [.. items];

    protected void Put(string cacheKey, ExpandoObject document)
    {
        cache[cacheKey] = document;
        index.MarkDirty(cacheKey);
    }

    protected void Evict(string cacheKey)
    {
        cache.TryRemove(cacheKey, out _);
        index.Remove(cacheKey);
    }

    protected string[] Find(string path, object value) => [.. index.Find(new PropertyPath(path), value, cache)];

    protected string[] Scan(string path, object value)
    {
        var segments = new PropertyPath(path).Segments.ToArray();
        return [.. cache.Where(entry => BulkStateValueMatch.TryFindValueInDocument(entry.Value, segments, 0, value)).Select(entry => entry.Key)];
    }
}

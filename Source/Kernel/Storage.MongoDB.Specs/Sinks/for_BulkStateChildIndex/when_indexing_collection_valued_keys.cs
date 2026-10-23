// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_indexing_collection_valued_keys : given.an_index
{
    readonly Dictionary<string, ExpandoObject> _originals = [];
    readonly List<(string[] Indexed, string[] Scanned)> _comparisons = [];

    void Establish()
    {
        _originals["array"] = Children(new[] { 42 });
        _originals["list"] = Children(new List<int> { 42 });
        _originals["jagged"] = Children((object)new[] { new[] { 42 } });
        _originals["dictionary"] = Children(new Dictionary<string, int[]> { ["value"] = [42] });
        foreach (var (key, document) in _originals)
        {
            Set(key, document);
        }
    }

    void Because()
    {
        PropertyPath path = "[children].id";
        foreach (var target in new object[] { "System.Int32[]", typeof(List<int>).ToString(), typeof(int[][]).ToString(), typeof(Dictionary<string, int[]>).ToString(), new[] { 42 } })
        {
            // Traverse PRE-copy documents in cache root order, not the copied values under test.
            var scanned = _states.Keys.Where(key => Sink.TryFindValueInDocument(_originals[key], path.Segments.ToArray(), 0, target)).ToArray();
            _comparisons.Add((Find(path, target), scanned));
        }

        _matches = Find(path, "System.Int32[]");
    }

    [Fact] void should_match_the_scan_of_the_original_documents() => _comparisons.ShouldEachConformTo(result => result.Indexed.SequenceEqual(result.Scanned));
    [Fact] void should_compare_every_collection_kind() => _comparisons.Count.ShouldEqual(5);
    [Fact] void should_find_the_array_valued_key() => _matches.ShouldContainOnly("array");
}

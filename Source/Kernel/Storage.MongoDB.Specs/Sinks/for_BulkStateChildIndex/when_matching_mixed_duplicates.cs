// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_matching_mixed_duplicates : given.an_index
{
    readonly List<(string[] Indexed, string[] Scanned)> _comparisons = [];

    void Establish()
    {
        Set("A", Children(0));
        Set("B", Children(0));
        var order = _states.Select(pair => pair.Key).ToArray();
        Set(order[0], Children(42L));
        Set(order[1], Children(42));
    }

    void Because()
    {
        Compare(42);
        Compare(42L);
        for (var root = 0; root < 512; root++)
        {
            Set($"root-{root}", Children(root % 2 == 0 ? (object)42L : 42));
        }

        Compare(42);
        Compare(42L);
        Remove("A");
        Set("B", Children(42L));
        Set("A", Children(42));
        Compare(42);
        Compare(42L);
    }

    [Fact] void should_preserve_the_scan_order_for_every_match() => _comparisons.ShouldEachConformTo(result => result.Indexed.SequenceEqual(result.Scanned));
    [Fact] void should_compare_before_and_after_growth_and_replacement() => _comparisons.Count.ShouldEqual(6);

    void Compare(object target)
    {
        PropertyPath path = "[children].id";
        var scanned = _states.Where(pair => Sink.TryFindValueInDocument(pair.Value, path.Segments.ToArray(), 0, target)).Select(pair => pair.Key).ToArray();
        _comparisons.Add((Find(path, target), scanned));
    }
}

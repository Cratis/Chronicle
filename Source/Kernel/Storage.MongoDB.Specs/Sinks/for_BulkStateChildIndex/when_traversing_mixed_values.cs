// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_traversing_mixed_values : given.an_index
{
    readonly List<(string[] Indexed, string[] Scanned)> _results = [];

    void Establish()
    {
        Set("nested", Document(("children", Document(("id", "child")))));
        Set("null", Document(("children", null)));
        Set("missing", Document());
        Set("scalar", Document(("children", "child")));
        Set("mixed", Document(("children", new object[] { "ignored", 42, Document(("id", "child")), Document(("id", null)) })));
    }

    void Because()
    {
        foreach (var name in new[] { "", "missing", "[children].id", "[children].missing", "[children].id.extra", "children" })
        {
            PropertyPath path = name;
            var scanned = _states.Where(pair => Sink.TryFindValueInDocument(pair.Value, path.Segments.ToArray(), 0, "child")).Select(pair => pair.Key).Order().ToArray();
            _results.Add((Find(path, "child").Order().ToArray(), scanned));
        }

        _matches = Find("[children].id", "child").Order().ToArray();
    }

    [Fact] void should_follow_the_original_traversal_rules() => _results.ShouldEachConformTo(result => result.Indexed.SequenceEqual(result.Scanned));
    [Fact] void should_find_both_expando_traversals() => _matches.ShouldContainOnly("mixed", "nested");
}

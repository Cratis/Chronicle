// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_comparing_with_the_linear_scan : given.an_index
{
    readonly List<(string[] Indexed, string[] Scanned)> _results = [];

    void Because()
    {
        var random = new Random(5264);
        var owners = new Dictionary<int, string>();
        PropertyPath path = "[children].id";
        var segments = path.Segments.ToArray();
        for (var operation = 0; operation < 200; operation++)
        {
            var root = $"root-{random.Next(8)}";
            var value = random.Next(24);
            if (random.Next(4) == 0)
            {
                foreach (var removed in owners.Where(pair => pair.Value == root).Select(pair => pair.Key).ToArray())
                {
                    owners.Remove(removed);
                }

                Remove(root);
            }
            else
            {
                owners[value] = root;
                foreach (var key in _states.Keys.Append(root).Distinct())
                {
                    Set(key, Children(owners.Where(pair => pair.Value == key).Select(pair => (object)pair.Key).ToArray()));
                }
            }

            for (var known = 0; known <= 24; known++)
            {
                var scanned = _states.Where(pair => Sink.TryFindValueInDocument(pair.Value, segments, 0, (long)known)).Select(pair => pair.Key).ToArray();
                _results.Add((Find(path, (long)known), scanned));
            }
        }
    }

    [Fact] void should_match_the_scan_after_every_operation() => _results.ShouldEachConformTo(result => result.Indexed.SequenceEqual(result.Scanned));
    [Fact] void should_exercise_every_lookup() => _results.Count.ShouldEqual(5000);
}

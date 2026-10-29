// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_BulkStateChildIndex;

public class when_running_a_randomized_sequence_of_operations : an_index_over_a_cache
{
    [Fact] void should_always_answer_like_the_scan()
    {
        var random = new Random(5264);
        object[] values = ["v0", "v1", "v2", 1, 1L, 2.0m, 2.00m, 0.0, -0.0, Guid.Empty, Guid.Empty.ToString()];
        string[] paths = ["joinKey", "children.childId", "children.leaves.leafId"];
        var mismatches = 0;

        object Value() => values[random.Next(values.Length)];
        ExpandoObject Leaf() => Doc(("leafId", Value()));
        ExpandoObject Child() => Doc(("childId", Value()), ("leaves", Items(Leaf(), Leaf())));
        ExpandoObject Root() => Doc(("joinKey", Value()), ("children", Items(Child(), Child())));

        for (var operation = 0; operation < 600; operation++)
        {
            var key = $"k{random.Next(12)}";
            switch (random.Next(5))
            {
                case 0:
                    Put(key, Root());
                    break;
                case 1:
                    if (cache.TryGetValue(key, out var lent))
                    {
                        index.MarkDirty(key);
                        ((IDictionary<string, object?>)lent)["children"] = Items(Child());
                    }

                    break;
                case 2:
                    Evict(key);
                    break;
                case 3:
                    if (cache.TryGetValue(key, out var unlent))
                    {
                        index.MarkDirty(key);
                        ((IDictionary<string, object?>)unlent)["joinKey"] = Value();
                    }

                    break;
                default:
                    var path = paths[random.Next(paths.Length)];
                    var target = Value();
                    if (!Find(path, target).SequenceEqual(Scan(path, target)))
                    {
                        mismatches++;
                    }

                    break;
            }
        }

        mismatches.ShouldEqual(0);
    }
}

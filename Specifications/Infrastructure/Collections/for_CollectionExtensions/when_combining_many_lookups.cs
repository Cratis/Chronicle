// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections.for_CollectionExtensions;

public class when_combining_many_lookups : Specification
{
    IEnumerable<ILookup<string, int>> lookups;
    ILookup<string, int> result;

    void Establish()
    {
        lookups = new[]
        {
            new[]
            {
                new { K = "1", V = 1 },
                new { K = "2", V = 2 },
                new { K = "2", V = 22 },
                new { K = "3", V = 3 },
            }.ToLookup(a => a.K, a => a.V),
            new[]
            {
                new { K = "1", V = 1 },
                new { K = "2", V = 2 },
            }.ToLookup(a => a.K, a => a.V),
            new[]
            {
                new { K = "2", V = 2 },
                new { K = "3", V = 333 },
            }.ToLookup(a => a.K, a => a.V),
            new[]
            {
                new { K = "", V = 88 },
                new { K = "4", V = 444 },
            }.ToLookup(a => a.K, a => a.V),
        };
    }

    void Because() => result = lookups.Combine();

    [Fact] void should_have_all_values() => result.Count.ShouldEqual(5);

    [Fact] void should_map_first_value() => result["1"].ShouldContainOnly(1, 1);

    [Fact] void should_map_second_value() => result["2"].ShouldContainOnly(2, 22, 2, 2);

    [Fact] void should_map_third_value() => result["3"].ShouldContainOnly(3, 333);

    [Fact] void should_map_fourth_value() => result["4"].ShouldContainOnly(444);

    [Fact] void should_map_empty_value() => result[""].ShouldContainOnly(88);
}

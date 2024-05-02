// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections.for_CollectionExtensions;

public class when_combining_one_lookup : Specification
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
            }.ToLookup(a => a.K, a => a.V)
        };
    }

    void Because() => result = lookups.Combine();

    [Fact] void should_have_two_values() => result.Count.ShouldEqual(2);

    [Fact] void should_map_first_value() => result["1"].ShouldContainOnly(1);

    [Fact] void should_map_second_value() => result["2"].ShouldContainOnly(2);
}

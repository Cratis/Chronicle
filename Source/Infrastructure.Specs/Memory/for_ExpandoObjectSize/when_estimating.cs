// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectSize;

public class when_estimating : Specification
{
    ExpandoObject _small;
    ExpandoObject _large;

    void Establish()
    {
        _small = new ExpandoObject();
        ((IDictionary<string, object?>)_small)["name"] = "short";

        var items = Enumerable.Range(0, 1000).Select(i =>
        {
            var item = new ExpandoObject();
            ((IDictionary<string, object?>)item)["name"] = new string('x', 100);
            return (object)item;
        }).ToArray();
        _large = new ExpandoObject();
        ((IDictionary<string, object?>)_large)["items"] = items;
    }

    [Fact] void should_count_the_characters_of_strings_at_two_bytes() => ExpandoObjectSize.Estimate(_small).ShouldBeGreaterThan(10L);
    [Fact] void should_grow_with_the_size_of_the_document() => ExpandoObjectSize.Estimate(_large).ShouldBeGreaterThan(200_000L);
    [Fact] void should_stay_in_proportion() => ExpandoObjectSize.Estimate(_large).ShouldBeLessThan(2_000_000L);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_a_key_with_the_parts_in_another_order : given.a_sink_with_composite_keys
{
    string? _found;

    async Task Because()
    {
        await Write(KeyOf("a", "b"), "name");
        var reordered = new System.Dynamic.ExpandoObject();
        ((IDictionary<string, object?>)reordered)["second"] = "b";
        ((IDictionary<string, object?>)reordered)["first"] = "a";
        _found = await NameOf(new(reordered, Cratis.Chronicle.Properties.ArrayIndexers.NoIndexers));
    }

    [Fact] void should_find_the_same_row() => _found.ShouldEqual("name");
}

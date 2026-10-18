// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_keys_that_collide_when_joined_with_an_underscore : given.a_sink_with_composite_keys
{
    async Task Because()
    {
        await Write(KeyOf("a_b", "c"), "first");
        await Write(KeyOf("a", "b_c"), "second");
    }

    [Fact] async Task should_store_two_rows() => (await _harness.ReadStoredRows()).Length.ShouldEqual(2);
    [Fact] async Task should_find_the_first_by_its_key() => (await NameOf(KeyOf("a_b", "c"))).ShouldEqual("first");
    [Fact] async Task should_find_the_second_by_its_key() => (await NameOf(KeyOf("a", "b_c"))).ShouldEqual("second");
}

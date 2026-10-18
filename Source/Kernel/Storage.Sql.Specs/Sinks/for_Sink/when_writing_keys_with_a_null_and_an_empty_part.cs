// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_keys_with_a_null_and_an_empty_part : given.a_sink_with_composite_keys
{
    async Task Because()
    {
        await Write(KeyOf("a", null), "null");
        await Write(KeyOf("a", string.Empty), "empty");
    }

    [Fact] async Task should_store_two_rows() => (await _harness.ReadStoredRows()).Length.ShouldEqual(2);
    [Fact] async Task should_keep_the_null_part_apart() => (await NameOf(KeyOf("a", null))).ShouldEqual("null");
    [Fact] async Task should_keep_the_empty_part_apart() => (await NameOf(KeyOf("a", string.Empty))).ShouldEqual("empty");
}

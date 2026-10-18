// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_a_key_stored_by_an_earlier_version : given.a_sink_with_composite_keys
{
    string? _foundBeforeWriting;

    async Task Because()
    {
        await _harness.StoreRowUnderId("a_b", "legacy");
        _foundBeforeWriting = await NameOf(KeyOf("a", "b"));
        await Write(KeyOf("a", "b"), "updated");
    }

    [Fact] void should_find_the_row_before_writing() => _foundBeforeWriting.ShouldEqual("legacy");
    [Fact] async Task should_not_add_a_row() => (await _harness.ReadStoredRows()).Length.ShouldEqual(1);
    [Fact] async Task should_rename_the_row_to_the_canonical_id() => (await _harness.ReadStoredRows()).Single().ContainsValue(CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["first"] = "a", ["second"] = "b" })).ShouldBeTrue();
    [Fact] async Task should_update_the_row() => (await NameOf(KeyOf("a", "b"))).ShouldEqual("updated");
}

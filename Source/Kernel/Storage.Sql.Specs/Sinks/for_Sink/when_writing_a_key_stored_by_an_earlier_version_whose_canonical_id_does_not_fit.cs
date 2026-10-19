// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_writing_a_key_stored_by_an_earlier_version_whose_canonical_id_does_not_fit : given.a_sink_with_composite_keys
{
    string? _foundBeforeWriting;
    string _first = new('a', 99);
    string _second = new('b', 99);

    async Task Because()
    {
        await _harness.StoreRowUnderId($"{_first}_{_second}", "legacy");
        _foundBeforeWriting = await NameOf(KeyOf(_first, _second));
        await Write(KeyOf(_first, _second), "updated");
    }

    [Fact] void should_find_the_row_before_writing() => _foundBeforeWriting.ShouldEqual("legacy");
    [Fact] async Task should_not_add_a_row() => (await _harness.ReadStoredRows()).Length.ShouldEqual(1);
    [Fact] async Task should_keep_the_legacy_id() => (await _harness.ReadStoredRows()).Single().ContainsValue($"{_first}_{_second}").ShouldBeTrue();
    [Fact] async Task should_update_the_row() => (await NameOf(KeyOf(_first, _second))).ShouldEqual("updated");
}

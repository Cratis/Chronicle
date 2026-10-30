// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key;

public class and_one_part_is_null_and_another_empty : given.a_sink_with_composite_keys
{
    IDictionary<string, object?>? _null;
    IDictionary<string, object?>? _empty;

    async Task Establish()
    {
        await _sink.ApplyChanges(CompositeKey(("a", null), ("b", "x")), ChangesetSettingAmount(1), 1UL);
        await _sink.ApplyChanges(CompositeKey(("a", string.Empty), ("b", "x")), ChangesetSettingAmount(2), 2UL);
    }

    async Task Because()
    {
        _null = await _sink.FindOrDefault(CompositeKey(("a", null), ("b", "x")));
        _empty = await _sink.FindOrDefault(CompositeKey(("a", string.Empty), ("b", "x")));
    }

    [Fact] void should_find_the_instance_with_the_null_part() => _null!["amount"].ShouldEqual(1);
    [Fact] void should_find_the_instance_with_the_empty_part() => _empty!["amount"].ShouldEqual(2);
}

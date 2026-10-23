// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key;

public class and_a_part_contains_the_separator : given.a_sink_with_composite_keys
{
    IDictionary<string, object?>? _underscoreInFirst;
    IDictionary<string, object?>? _underscoreInSecond;
    IDictionary<string, object?>? _tilde;
    IDictionary<string, object?>? _colon;

    async Task Establish()
    {
        await _sink.ApplyChanges(CompositeKey(("a", "x_y"), ("b", "z")), ChangesetSettingAmount(1), 1UL);
        await _sink.ApplyChanges(CompositeKey(("a", "x"), ("b", "y_z")), ChangesetSettingAmount(2), 2UL);
        await _sink.ApplyChanges(CompositeKey(("a", "1:x"), ("b", "~")), ChangesetSettingAmount(3), 3UL);
    }

    async Task Because()
    {
        _underscoreInFirst = await _sink.FindOrDefault(CompositeKey(("a", "x_y"), ("b", "z")));
        _underscoreInSecond = await _sink.FindOrDefault(CompositeKey(("a", "x"), ("b", "y_z")));
        _tilde = await _sink.FindOrDefault(CompositeKey(("a", "1:x"), ("b", "~")));
        _colon = await _sink.FindOrDefault(CompositeKey(("a", "1"), ("b", "x:~")));
    }

    [Fact] void should_find_the_instance_with_the_underscore_in_the_first_part() => _underscoreInFirst!["amount"].ShouldEqual(1);
    [Fact] void should_find_the_instance_with_the_underscore_in_the_second_part() => _underscoreInSecond!["amount"].ShouldEqual(2);
    [Fact] void should_find_the_instance_with_the_colon_and_tilde() => _tilde!["amount"].ShouldEqual(3);
    [Fact] void should_not_confuse_a_shifted_colon_with_another_instance() => _colon.ShouldBeNull();
}

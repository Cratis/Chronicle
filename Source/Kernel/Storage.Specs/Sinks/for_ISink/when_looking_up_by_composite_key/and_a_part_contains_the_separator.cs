// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

public abstract class and_a_part_contains_the_separator<THarness> : given.a_composite_key_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    CompositeKeyLookupResult _first;
    CompositeKeyLookupResult _second;

    async Task Establish()
    {
        await _sink.ApplyChanges(CompositeKey("x_y", "z"), ChangesetSettingCountTo(1), 42UL);
        await _sink.ApplyChanges(CompositeKey("x", "other_z"), ChangesetSettingCountTo(2), 43UL);
    }

    async Task Because()
    {
        _first = await Lookup(CompositeKey("x_y", "z"));
        _second = await Lookup(CompositeKey("x", "other_z"));
    }

    [Fact] public void should_find_the_first_instance() => _first.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] public void should_read_the_first_instance() => Count(_first).ShouldEqual(1);
    [Fact] public void should_find_the_second_instance() => _second.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] public void should_read_the_second_instance() => Count(_second).ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

public abstract class and_parts_are_given_in_another_order<THarness> : given.a_composite_key_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    CompositeKeyLookupResult _result;

    async Task Establish() => await _sink.ApplyChanges(CompositeKey("a", "b"), ChangesetSettingCountTo(42), 42UL);

    async Task Because() => _result = await Lookup(CompositeKey("a", "b", reverse: true));

    [Fact] public void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] public void should_read_the_written_instance() => Count(_result).ShouldEqual(42);
}

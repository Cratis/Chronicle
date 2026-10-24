// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

public abstract class and_a_replay_is_in_progress<THarness> : given.a_composite_key_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    CompositeKeyLookupResult _buffered;
    CompositeKeyLookupResult _primary;
    CompositeKeyLookupResult _missing;

    async Task Establish()
    {
        await _sink.ApplyChanges(CompositeKey("a", "b"), ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(CompositeKey("a", "b"), ChangesetSettingCountTo(2), 43UL);
        await _sink.ApplyChanges(CompositeKey("replay", "only"), ChangesetSettingCountTo(3), 44UL);
    }

    async Task Because()
    {
        _buffered = await Lookup(CompositeKey("a", "b"));
        await _sink.EndBulk();
        _primary = await Lookup(CompositeKey("a", "b"));
        _missing = await Lookup(CompositeKey("replay", "only"));
    }

    [Fact] public void should_not_read_buffered_replay_state() => Count(_buffered).ShouldEqual(1);
    [Fact] public void should_read_the_primary() => Count(_primary).ShouldEqual(1);
    [Fact] public void should_not_find_a_replay_only_instance() => _missing.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
}

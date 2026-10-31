// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_in_primary;

public abstract class and_a_replay_is_in_progress<THarness> : given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    static readonly Key _replayOnlyKey = new("replay-only", ArrayIndexers.NoIndexers);
    ExpandoObject? _primary;
    ExpandoObject? _primaryWhileBuffered;
    ExpandoObject? _replayed;
    ExpandoObject? _missing;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL);
        await _sink.ApplyChanges(_replayOnlyKey, ChangesetSettingCountTo(3), 44UL);
    }

    async Task Because()
    {
        _primaryWhileBuffered = await _sink.FindInPrimaryOrDefault(_key);
        await _sink.EndBulk();
        _primary = await _sink.FindInPrimaryOrDefault(_key);
        _replayed = await _sink.FindOrDefault(_key);
        _missing = await _sink.FindInPrimaryOrDefault(_replayOnlyKey);
    }

    [Fact] public void should_not_read_buffered_replay_state() => Count(_primaryWhileBuffered).ShouldEqual(1);
    [Fact] public void should_read_the_pre_replay_state() => Count(_primary).ShouldEqual(1);
    [Fact] public void should_keep_the_projection_read_on_replay_state() => Count(_replayed).ShouldEqual(2);
    [Fact] public void should_not_find_a_key_present_only_in_the_replay() => _missing.ShouldBeNull();

    static int Count(ExpandoObject? instance) => Convert.ToInt32(((IDictionary<string, object?>)instance!)["count"], CultureInfo.InvariantCulture);
}

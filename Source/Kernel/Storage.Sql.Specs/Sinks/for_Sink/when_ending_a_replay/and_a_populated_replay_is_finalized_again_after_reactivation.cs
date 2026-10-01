// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_a_populated_replay_is_finalized_again_after_reactivation : given.two_sinks_for_one_read_model
{
    int? _count;
    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _sink.EndReplay(ReplayContext() with { AllowEmptyResult = true });
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
    }
    async Task Because()
    {
        await _otherSink.EndReplay(ReplayContext() with { AllowEmptyResult = true });
        _count = await CurrentCountOrNull();
    }
    [Fact] void should_not_manufacture_and_promote_an_empty_shadow() => _count.ShouldEqual(3);
}

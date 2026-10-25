// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_stored_pin_history_shares_the_content_fence : given.a_fenced_history_release
{
    async Task Because() => await ReleaseEvent();

    [Fact] void should_release_all_history() => HistoryIsReleased().ShouldBeTrue();
    [Fact] void should_release_content_and_three_history_payloads() => _releaseCalls.ShouldEqual(4);
    [Fact] void should_share_one_fence_between_content_and_history() => _fenceReadsDuringRelease.ShouldEqual(1);
    [Fact] void should_read_a_fresh_fence_after_delivery() => _fenceReads.ShouldEqual(2);
}

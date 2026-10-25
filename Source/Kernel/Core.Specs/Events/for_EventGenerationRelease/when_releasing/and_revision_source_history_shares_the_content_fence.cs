// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_revision_source_history_shares_the_content_fence : given.a_fenced_history_release
{
    void Establish() => _event = _event with
    {
        RevisedGeneration = 2,
        Revisions = [new(2, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred, "{\"fullName\":\"ciphertext\"}")]
    };

    async Task Because() => await ReleaseEvent();

    [Fact] void should_deliver_the_revised_pin() => _result[0].Context.EventType.ShouldEqual(_pin);
    [Fact] void should_release_all_history() => HistoryIsReleased().ShouldBeTrue();
    [Fact] void should_release_content_and_four_history_payloads() => _releaseCalls.ShouldEqual(5);
    [Fact] void should_share_one_fence_between_content_and_history() => _fenceReadsDuringRelease.ShouldEqual(1);
    [Fact] void should_read_a_fresh_fence_after_delivery() => _fenceReads.ShouldEqual(2);
}

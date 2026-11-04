// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberResultDeferrals;

public class when_completing_a_deferral_that_knows_where_to_resume : Specification
{
    ObserverSubscriberResult _result;

    void Because() => _result = ObserverSubscriberResultDeferrals
        .Deferred(4UL, TimeSpan.FromSeconds(10), DateTimeOffset.UnixEpoch)
        .ResumingDeferralWithin([AppendedEvent.EmptyWithEventSequenceNumber(3), AppendedEvent.EmptyWithEventSequenceNumber(4), AppendedEvent.EmptyWithEventSequenceNumber(5)]);

    [Fact] void should_resume_after_the_last_handled_event() => _result.Deferral!.ResumeFrom.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_keep_what_was_handled() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_wait_until_the_requested_time() => _result.Deferral!.Until.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(10));
}

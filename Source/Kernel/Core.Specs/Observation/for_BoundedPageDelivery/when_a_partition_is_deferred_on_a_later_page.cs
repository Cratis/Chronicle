// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_BoundedPageDelivery;

public class when_a_partition_is_deferred_on_a_later_page : given.a_page_beyond_its_bounds
{
    static readonly DateTimeOffset _now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    ObserverPageResult _result;

    async Task Because() => _result = await BoundedPageDelivery.Deliver(_bounds, _batches, page => Answer(page, batch =>
        _delivered.Count == 2 && batch.Partition == _first
            ? ObserverSubscriberResultDeferrals.Deferred(EventSequenceNumber.Unavailable, TimeSpan.FromMinutes(1), _now).ResumingDeferralWithin(batch.Events)
            : HandledAll(batch)));

    [Fact] void should_report_the_partition_as_deferred() => _result.Results[_first].State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_keep_what_the_earlier_page_handled() => _result.Results[_first].LastSuccessfulObservation.Value.ShouldEqual(2UL);
    [Fact] void should_resume_from_the_first_event_of_the_later_page() => _result.Results[_first].Deferral!.ResumeFrom.Value.ShouldEqual(3UL);
    [Fact] void should_settle_the_other_partition_on_its_own() => _result.Results[_second].LastSuccessfulObservation.Value.ShouldEqual(4UL);
}

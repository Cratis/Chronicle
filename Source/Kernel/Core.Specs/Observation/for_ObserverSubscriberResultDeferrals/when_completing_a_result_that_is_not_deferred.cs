// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberResultDeferrals;

public class when_completing_a_result_that_is_not_deferred : Specification
{
    ObserverSubscriberResult _original;
    ObserverSubscriberResult _result;

    void Establish() => _original = ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Failed");

    void Because() => _result = _original.ResumingDeferralWithin([AppendedEvent.EmptyWithEventSequenceNumber(7)]);

    [Fact] void should_leave_it_as_it_is() => _result.ShouldEqual(_original);
}

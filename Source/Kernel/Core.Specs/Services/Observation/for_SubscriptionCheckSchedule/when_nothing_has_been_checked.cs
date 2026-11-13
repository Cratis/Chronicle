// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule;

public class when_nothing_has_been_checked : given.a_schedule
{
    void Because() => _next = _schedule.NextCheckAfter(Now);

    [Fact] void should_check_after_the_revise_interval() => _next.ShouldEqual(Revise);
}

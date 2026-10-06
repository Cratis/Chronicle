// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationBackoff;

public class when_computing_the_next_delay_after_many_failures : Specification
{
    RegistrationBackoff _backoff;

    void Establish() => _backoff = new RegistrationBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), () => 1);

    [Fact] void should_cap_at_the_maximum_delay_past_the_timespan_range() => _backoff.NextDelay(64).ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_cap_at_the_maximum_delay_for_the_largest_failure_count() => _backoff.NextDelay(int.MaxValue).ShouldEqual(TimeSpan.FromMinutes(1));
}

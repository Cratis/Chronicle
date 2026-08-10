// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationBackoff;

public class when_computing_the_next_delay : Specification
{
    RegistrationBackoff _backoff;

    void Establish() => _backoff = new RegistrationBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8), () => 1);

    [Fact] void should_not_delay_when_nothing_has_failed() => _backoff.NextDelay(0).ShouldEqual(TimeSpan.Zero);
    [Fact] void should_start_at_the_initial_delay_after_the_first_failure() => _backoff.NextDelay(1).ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_grow_exponentially_with_consecutive_failures() => _backoff.NextDelay(3).ShouldEqual(TimeSpan.FromSeconds(4));
    [Fact] void should_cap_at_the_maximum_delay() => _backoff.NextDelay(10).ShouldEqual(TimeSpan.FromSeconds(8));
}

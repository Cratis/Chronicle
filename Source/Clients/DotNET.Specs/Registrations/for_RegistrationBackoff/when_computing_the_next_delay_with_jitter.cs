// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationBackoff;

public class when_computing_the_next_delay_with_jitter : Specification
{
    RegistrationBackoff _floorBackoff;
    RegistrationBackoff _midBackoff;

    void Establish()
    {
        _floorBackoff = new RegistrationBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8), () => 0);
        _midBackoff = new RegistrationBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8), () => 0.5);
    }

    [Fact] void should_never_drop_below_half_the_exponential_delay() => _floorBackoff.NextDelay(1).ShouldEqual(TimeSpan.FromMilliseconds(500));
    [Fact] void should_scale_the_exponential_delay_by_the_jitter() => _midBackoff.NextDelay(1).ShouldEqual(TimeSpan.FromMilliseconds(750));
    [Fact] void should_jitter_the_capped_delay_as_well() => _floorBackoff.NextDelay(10).ShouldEqual(TimeSpan.FromSeconds(4));
}

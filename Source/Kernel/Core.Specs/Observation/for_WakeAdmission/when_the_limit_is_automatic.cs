// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WakeAdmission;

public class when_the_limit_is_automatic : Specification
{
    WakeAdmission _admission;

    void Because() => _admission = new(0, 3);

    [Fact] void should_be_two_per_processor_with_a_floor_of_four() => _admission.Limit.ShouldEqual(6);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WakeAdmission;

public class when_the_limit_is_configured : Specification
{
    WakeAdmission _admission;

    void Because() => _admission = new(7, 2);

    [Fact] void should_use_the_configured_limit() => _admission.Limit.ShouldEqual(7);
}

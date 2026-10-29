// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceTargetPolicy;

public class when_reporting_a_violation : given.a_capture_target_policy
{
    void Because() => _result = _policy.CheckAuthority(new Uri("https://capture.example"), null);

    [Fact] void should_name_the_capture_settings() => _result.Reason.ShouldEqual("Capture source target host is not in ExternalServices:AllowedHosts for this event store.");
}

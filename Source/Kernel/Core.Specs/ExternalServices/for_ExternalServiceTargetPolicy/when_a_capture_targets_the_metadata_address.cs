// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceTargetPolicy;

public class when_a_capture_targets_the_metadata_address : given.a_capture_target_policy
{
    void Because() => _result = _policy.CheckAuthority(new Uri("http://169.254.169.254"), null);

    [Fact] void should_reject_the_target() => _result.IsAllowed.ShouldBeFalse();
}

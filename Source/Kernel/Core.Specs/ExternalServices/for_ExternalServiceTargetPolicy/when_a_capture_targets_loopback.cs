// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceTargetPolicy;

public class when_a_capture_targets_loopback : given.a_capture_target_policy
{
    void Establish() => Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });

    void Because() => _result = _policy.CheckAddresses(new Uri("https://capture.example"), [IPAddress.Loopback], null);

    [Fact] void should_reject_the_target() => _result.IsAllowed.ShouldBeFalse();
}

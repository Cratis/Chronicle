// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceTargetPolicy;

public class when_a_capture_host_is_allow_listed_for_its_event_store : given.a_capture_target_policy
{
    void Establish() => Configure(new()
    {
        Validation = WebhookValidationMode.BlockLocalAddresses,
        EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["orders-api"] }
    });

    void Because() => _result = _policy.CheckAddresses(new Uri("https://orders-api"), [IPAddress.Loopback], "orders");

    [Fact] void should_allow_the_target() => _result.IsAllowed.ShouldBeTrue();
}

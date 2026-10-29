// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_checking_resolved_addresses;

public class and_a_private_address_resolves_in_block_local_mode : given.a_target_policy
{
    void Establish() => Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });

    void Because() => _result = _policy.CheckAddresses(new Uri("https://receiver.example"), [IPAddress.Parse("10.1.2.3")], null);

    [Fact] void should_allow_the_address() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_report_the_reason() => _result.Reason.ShouldEqual("Webhook target resolves to a private or special address.");
}

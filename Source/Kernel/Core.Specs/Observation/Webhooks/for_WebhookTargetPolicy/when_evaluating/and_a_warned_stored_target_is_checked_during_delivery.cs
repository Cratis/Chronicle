// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.when_evaluating;

public class and_a_warned_stored_target_is_checked_during_delivery : given.a_target_policy
{
    void Establish() => Configure(new() { Validation = WebhookValidationMode.Warn });

    async Task Because()
    {
        _result = await _policy.Evaluate("http://receiver.example");
        for (var delivery = 0; delivery < 3; delivery++)
        {
            await _policy.EvaluateForDelivery("http://receiver.example");
            _policy.CheckAuthority(new Uri("http://receiver.example"), null);
            _policy.CheckAddresses(new Uri("http://receiver.example"), [IPAddress.Loopback], null);
        }
    }

    [Fact] void should_allow_the_stored_target() => _result.IsAllowed.ShouldBeTrue();
    [Fact] void should_log_only_the_activation_warning() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log").ShouldEqual(1);
}

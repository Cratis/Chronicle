// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.when_sending;

public class and_context_is_missing : given.a_validation_handler
{
    void Establish()
    {
        Configure(new()
        {
            Validation = WebhookValidationMode.Enforce,
            AllowedHosts = ["global.example"],
            EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["store.example"] }
        });
        CreateClient("https://global.example");
    }

    Task Because() => Send();

    [Fact] void should_log_the_global_fallback() => _handlerLogger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log").ShouldBeTrue();
    [Fact] void should_use_global_policy() => _transport.Calls.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.when_sending;

public class and_a_store_has_an_override : given.a_validation_handler
{
    void Establish()
    {
        Configure(new()
        {
            Validation = WebhookValidationMode.Enforce,
            EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["store.example"] }
        });
        CreateClient("https://store.example");
        _request.Options.Set(WebhookRequestOptions.EventStore, "orders");
    }

    Task Because() => Send();

    [Fact] void should_use_the_store_policy() => _transport.Calls.ShouldEqual(1);
    [Fact] void should_not_reject_the_request() => _error.ShouldBeNull();
}

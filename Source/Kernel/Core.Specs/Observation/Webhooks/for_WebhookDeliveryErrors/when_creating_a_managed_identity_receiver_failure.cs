// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDeliveryErrors;

public class when_creating_a_managed_identity_receiver_failure : Specification
{
    WebhookDeliveryFailed _error;

    void Because()
    {
        var target = new WebhookTarget("https://receiver.invalid/", new ManagedIdentityAuthorization("identity", "scope"), new Dictionary<string, string>());
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden) { ReasonPhrase = "receiver-secret" };
        _error = WebhookDeliveryErrors.ForResponse(target, response);
    }

    [Fact] void should_omit_receiver_reason() => _error.ReasonPhrase.ShouldBeNull();
    [Fact] void should_preserve_status() => _error.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDeliveryErrors;

public class when_creating_a_managed_identity_transport_failure : Specification
{
    Exception _error;

    void Because()
    {
        var target = new WebhookTarget("https://receiver.invalid/", new ManagedIdentityAuthorization("identity", "scope"), new Dictionary<string, string>());
        _error = WebhookDeliveryErrors.SafeError(target, new HttpRequestException("receiver-secret"));
    }

    [Fact] void should_use_managed_identity_failure() => _error.ShouldBeOfExactType<ManagedIdentityWebhookDeliveryFailed>();
    [Fact] void should_not_retain_inner_exception() => _error.InnerException.ShouldBeNull();
    [Fact] void should_not_retain_raw_message() => _error.ToString().ShouldNotContain("receiver-secret");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDeliveryErrors;

public class when_managed_identity_is_not_allowed : Specification
{
    readonly ManagedIdentityWebhookNotAllowed _error = new();
    WebhookDeliveryResult _result;

    void Because() => _result = WebhookDeliveryErrors.FromException(new("https://receiver.invalid/", WebhookAuthorization.None, new Dictionary<string, string>()), _error, null);

    [Fact] void should_keep_failure_identity() => _result.Error.ShouldEqual(_error);
    [Fact] void should_pause_observer() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.PauseObserver);
}

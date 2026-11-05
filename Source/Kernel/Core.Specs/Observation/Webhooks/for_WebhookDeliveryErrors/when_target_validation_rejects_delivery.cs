// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDeliveryErrors;

public class when_target_validation_rejects_delivery : Specification
{
    readonly WebhookTargetRejected _rejection = new("Webhook target is not allowed.");
    WebhookDeliveryResult _result;

    void Because() => _result = WebhookDeliveryErrors.FromException(new("https://receiver.invalid/", WebhookAuthorization.None, new Dictionary<string, string>()), new HttpRequestException("raw"), _rejection);

    [Fact] void should_keep_rejection_identity() => _result.Error.ShouldEqual(_rejection);
    [Fact] void should_pause_observer() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.PauseObserver);
}

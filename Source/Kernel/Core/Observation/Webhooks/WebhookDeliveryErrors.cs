// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Creates recordable delivery failures without retaining managed identity tokens from transport errors or receiver responses.
/// </summary>
static class WebhookDeliveryErrors
{
    internal static WebhookDeliveryResult FromException(WebhookTarget target, Exception error, WebhookTargetRejected? rejection)
    {
        if (rejection is not null)
        {
            return new(EventSequenceNumber.Unavailable, rejection) { FailurePolicy = WebhookDeliveryFailurePolicy.PauseObserver };
        }
        return error is ManagedIdentityWebhookNotAllowed
            ? new(EventSequenceNumber.Unavailable, error) { FailurePolicy = WebhookDeliveryFailurePolicy.PauseObserver }
            : new(EventSequenceNumber.Unavailable, SafeError(target, error));
    }

    internal static Exception SafeError(WebhookTarget target, Exception error) => target.Authorization.IsT4
        ? new ManagedIdentityWebhookDeliveryFailed()
        : error;

    internal static WebhookDeliveryFailed ForResponse(WebhookTarget target, HttpResponseMessage response) =>
        new(target.Url, response.StatusCode, target.Authorization.IsT4 ? null : response.ReasonPhrase);
}

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
    /// <summary>
    /// Creates the delivery result for an exception raised while delivering to a webhook.
    /// </summary>
    /// <param name="target">The <see cref="WebhookTarget"/> being delivered to.</param>
    /// <param name="error">The <see cref="Exception"/> that occurred.</param>
    /// <param name="rejection">The optional <see cref="WebhookTargetRejected"/> when the target policy refused the target.</param>
    /// <returns>The <see cref="WebhookDeliveryResult"/> to record.</returns>
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

    /// <summary>
    /// Gets an error that is safe to record for the target, replacing errors for managed identity targets so no token is retained.
    /// </summary>
    /// <param name="target">The <see cref="WebhookTarget"/> being delivered to.</param>
    /// <param name="error">The <see cref="Exception"/> that occurred.</param>
    /// <returns>The <see cref="Exception"/> to record.</returns>
    internal static Exception SafeError(WebhookTarget target, Exception error) => target.Authorization.IsT4
        ? new ManagedIdentityWebhookDeliveryFailed()
        : error;

    /// <summary>
    /// Creates the failure for an unsuccessful receiver response, omitting the reason phrase for managed identity targets.
    /// </summary>
    /// <param name="target">The <see cref="WebhookTarget"/> being delivered to.</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/> from the receiver.</param>
    /// <returns>The <see cref="WebhookDeliveryFailed"/> to record.</returns>
    internal static WebhookDeliveryFailed ForResponse(WebhookTarget target, HttpResponseMessage response) =>
        new(target.Url, response.StatusCode, target.Authorization.IsT4 ? null : response.ReasonPhrase);
}

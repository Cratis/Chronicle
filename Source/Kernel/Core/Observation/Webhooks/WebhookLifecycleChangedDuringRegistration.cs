// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when the lifecycle of a webhook changed while an envelope registration was being admitted.
/// </summary>
/// <param name="webhookId">The identifier of the webhook.</param>
public class WebhookLifecycleChangedDuringRegistration(string webhookId)
    : Exception($"The lifecycle of webhook '{webhookId}' changed during registration, so it was not added with delivery format EnvelopeV1. Retry the registration.");

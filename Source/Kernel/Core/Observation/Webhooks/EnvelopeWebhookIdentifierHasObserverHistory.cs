// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when an envelope webhook is added under an identifier whose observer already has history.
/// </summary>
/// <param name="webhookId">The identifier of the webhook.</param>
public class EnvelopeWebhookIdentifierHasObserverHistory(string webhookId)
    : Exception($"Webhook '{webhookId}' cannot be added with delivery format EnvelopeV1 because observer '{webhookId}' already has history (position, failed partitions or replay state) from an earlier registration. Use a new identifier, or remove the observer first.");

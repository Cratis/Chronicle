// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a registration tries to change the delivery format of an existing webhook.
/// </summary>
/// <param name="webhookId">The identifier of the webhook.</param>
/// <param name="current">The delivery format the webhook was created with.</param>
/// <param name="requested">The delivery format requested.</param>
public class WebhookDeliveryFormatCannotBeChanged(string webhookId, WebhookDeliveryFormat current, WebhookDeliveryFormat requested)
    : Exception($"Webhook '{webhookId}' was created with delivery format {current}. The delivery format of an existing webhook cannot be changed (requested {requested}). Register a new webhook with a new identifier to use {requested}.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when a registration requests an unsupported webhook delivery format.
/// </summary>
/// <param name="webhookId">The identifier of the webhook.</param>
/// <param name="format">The requested delivery format.</param>
public class WebhookDeliveryFormatNotSupported(string webhookId, WebhookDeliveryFormat format)
    : Exception($"Webhook '{webhookId}' requests delivery format {format}, which is not supported.");

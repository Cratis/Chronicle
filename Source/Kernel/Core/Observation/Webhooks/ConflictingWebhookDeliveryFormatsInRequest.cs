// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when one registration contains the same webhook with different delivery formats.
/// </summary>
/// <param name="webhookId">The identifier of the webhook.</param>
/// <param name="formats">The delivery formats requested for the webhook.</param>
public class ConflictingWebhookDeliveryFormatsInRequest(string webhookId, IEnumerable<string> formats)
    : Exception($"Webhook '{webhookId}' appears more than once in the registration with different delivery formats ({string.Join(", ", formats)}). Register each webhook once.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The exception that is thrown when a receiver result is unusable and acknowledges nothing.
/// </summary>
/// <param name="targetUrl">The URL of the webhook target.</param>
/// <param name="reason">The fixed kernel description of the malformed result.</param>
public class DeliveryResultMalformed(WebhookTargetUrl targetUrl, string reason)
    : Exception($"Webhook delivery result from '{targetUrl}' acknowledges nothing because {reason}.");

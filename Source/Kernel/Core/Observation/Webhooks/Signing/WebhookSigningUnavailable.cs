// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// The exception that is thrown when a webhook delivery cannot be signed safely.
/// </summary>
public class WebhookSigningUnavailable() : Exception("Webhook delivery signing is unavailable.");

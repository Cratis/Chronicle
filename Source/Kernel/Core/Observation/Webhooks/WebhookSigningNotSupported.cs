// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception thrown when registration requests an unsupported signing policy.
/// </summary>
/// <param name="signing">The requested signing policy.</param>
public class WebhookSigningNotSupported(WebhookSigningMode signing) : Exception($"Webhook signing mode '{signing}' is not supported. Use Unspecified, Disabled or Ed25519.");

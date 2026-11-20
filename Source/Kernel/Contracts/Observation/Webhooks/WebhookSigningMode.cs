// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the requested signing policy for webhook registration.
/// </summary>
public enum WebhookSigningMode
{
    /// <summary>
    /// Preserve the current policy, or use no signing for a new webhook.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Deliver without signing headers.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Sign deliveries using Ed25519.
    /// </summary>
    Ed25519 = 2
}

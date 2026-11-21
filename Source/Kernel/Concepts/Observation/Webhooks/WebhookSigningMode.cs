// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the signing policy for outgoing webhook deliveries.
/// </summary>
public enum WebhookSigningMode
{
    /// <summary>
    /// Deliver without Standard Webhooks signing headers, preserving existing behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Require Standard Webhooks Ed25519 signatures using the event store's keys.
    /// </summary>
    Ed25519 = 1,

    /// <summary>
    /// Reserved for HMAC-SHA256 signing, which is not supported yet.
    /// </summary>
    HmacSha256 = 2
}

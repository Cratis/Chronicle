// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the algorithm a webhook signing key is used with.
/// </summary>
public enum WebhookSigningAlgorithm
{
    /// <summary>
    /// An Ed25519 asymmetric key pair, with one key ring per event store.
    /// </summary>
    Ed25519 = 0,

    /// <summary>
    /// A symmetric HMAC-SHA256 secret, scoped to a webhook.
    /// </summary>
    HmacSha256 = 1
}

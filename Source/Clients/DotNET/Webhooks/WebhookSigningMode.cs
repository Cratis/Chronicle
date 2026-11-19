// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines how webhook deliveries are signed.
/// </summary>
public enum WebhookSigningMode
{
    /// <summary>
    /// Deliver without a signature.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sign deliveries with the event store's Ed25519 key using Standard Webhooks.
    /// </summary>
    Ed25519 = 1
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines the status of a webhook signing key that Chronicle publishes.
/// </summary>
public enum WebhookSigningKeyStatus
{
    /// <summary>
    /// The key is the current signing key.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The key was rotated out and still signs deliveries until its overlap ends.
    /// </summary>
    Overlap = 1
}

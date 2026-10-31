// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// The rollout mode for webhook target validation.
/// </summary>
public enum WebhookValidationMode
{
    /// <summary>
    /// Log policy violations without blocking delivery, except for metadata addresses.
    /// </summary>
    Warn = 0,

    /// <summary>
    /// Reject targets that violate policy.
    /// </summary>
    Enforce = 1,

    /// <summary>
    /// Block local destinations and warn about other policy violations.
    /// </summary>
    BlockLocalAddresses = 2,

    /// <summary>
    /// Block private and special destinations and warn about other policy violations.
    /// </summary>
    BlockPrivateAddresses = 3
}

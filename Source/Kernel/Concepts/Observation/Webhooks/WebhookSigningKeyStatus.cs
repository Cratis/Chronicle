// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the lifecycle status of a webhook signing key.
/// </summary>
public enum WebhookSigningKeyStatus
{
    /// <summary>
    /// The key is the one new deliveries are signed with.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The key has been superseded but is still valid until its overlap window ends.
    /// </summary>
    Overlap = 1,

    /// <summary>
    /// The key is no longer valid and its private material has been erased.
    /// </summary>
    Retired = 2
}

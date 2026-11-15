// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents whether the policy of a subject currently lets its links through.
/// </summary>
/// <remarks>
/// The values are explicit and never renumbered. <see cref="Active"/> is the only status that admits a peer.
/// </remarks>
public enum FederationPolicyStatus
{
    /// <summary>
    /// The policy is enabled.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The policy is temporarily disabled and can be resumed.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The policy is permanently disabled. The subject has to be given a new id to be admitted again.
    /// </summary>
    Revoked = 2
}

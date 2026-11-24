// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents where an accepted registration is in its life.
/// </summary>
public enum RegistrationState
{
    /// <summary>
    /// The kernel has accepted the registration and has not applied it yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The registration has been applied, or there is nothing outstanding.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// The registration could not be applied; the reason is in the status.
    /// </summary>
    Failed = 2
}

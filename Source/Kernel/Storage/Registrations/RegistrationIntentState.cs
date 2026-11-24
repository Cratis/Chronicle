// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Registrations;

/// <summary>
/// Where a registration intent is in its life.
/// </summary>
public enum RegistrationIntentState
{
    /// <summary>
    /// The kernel has accepted the registration and has not applied it yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The registration has been applied.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// The registration could not be applied; the reason is recorded on the intent.
    /// </summary>
    Failed = 2
}

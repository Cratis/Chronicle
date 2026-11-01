// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes registration precedence across event sequences for a connected-client observer.
/// </summary>
public enum RegistrationPrecedenceOutcome
{
    /// <summary>
    /// No connected client owns the observer on another event sequence.
    /// </summary>
    NoConflict = 0,

    /// <summary>
    /// The registering client has a higher application version than the owner and takes the observer.
    /// </summary>
    IncomingIsNewer = 1,

    /// <summary>
    /// The owner has a higher application version; the registering client stays on standby.
    /// </summary>
    OwnerIsNewer = 2,

    /// <summary>
    /// The versions are equal or unknown; the last registration wins and the switch is reported.
    /// </summary>
    Unordered = 3
}

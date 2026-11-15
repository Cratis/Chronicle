// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Represents the outcome of claiming or renewing the cluster-wide reservation of a connection.
/// </summary>
enum ReservationOutcome
{
    /// <summary>
    /// The connection is reserved for the caller.
    /// </summary>
    Granted = 0,

    /// <summary>
    /// The connection is reserved for another caller.
    /// </summary>
    Refused = 1,

    /// <summary>
    /// The reservation could not be established or verified.
    /// </summary>
    Failed = 2
}

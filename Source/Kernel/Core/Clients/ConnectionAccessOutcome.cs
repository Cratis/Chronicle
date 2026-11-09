// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// The result of verifying access to an existing connection.
/// </summary>
public enum ConnectionAccessOutcome
{
    /// <summary>The caller holds an active connection.</summary>
    Granted = 0,

    /// <summary>The caller does not hold an active connection.</summary>
    Denied = 1,

    /// <summary>The cluster could not verify the connection.</summary>
    Unverifiable = 2
}

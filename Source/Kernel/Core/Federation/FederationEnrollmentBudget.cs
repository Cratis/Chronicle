// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents what a request to the enrollment routes counts against, so that one kind of request cannot use up the budget of another.
/// </summary>
public enum FederationEnrollmentBudget
{
    /// <summary>
    /// Fetching the authority certificate and redeeming a code. Both are made by a peer that is not yet enrolled.
    /// </summary>
    Discovery = 0,

    /// <summary>
    /// Renewing a certificate. Made by an enrolled peer, so a flood of unenrolled requests must not stop peers from renewing.
    /// </summary>
    Renewal = 1
}

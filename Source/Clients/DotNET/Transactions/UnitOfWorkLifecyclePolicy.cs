// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Determines what happens when events are staged after a unit of work begins completing.
/// </summary>
public enum UnitOfWorkLifecyclePolicy
{
    /// <summary>
    /// Retains legacy staging behavior for units without decision reads, while logging every late attempt.
    /// </summary>
    Compatibility = 0,

    /// <summary>
    /// Rejects all late staging before reading or changing the supplied events.
    /// </summary>
    Strict = 1
}

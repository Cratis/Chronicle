// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines whether an observer records its subscription in its state document (decision 0034).
/// </summary>
public enum DurableSubscriptionMode
{
    /// <summary>
    /// Record no subscription and clear any that is stored.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Record the subscription and compare it with the live ones, without acting on the result.
    /// </summary>
    Shadow = 1
}

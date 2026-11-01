// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents per-node password sign-in throttling settings.
/// </summary>
public class SignInThrottling
{
    /// <summary>
    /// Gets the number of failures per username/address before lockout. Zero disables throttling.
    /// </summary>
    public int MaxFailedAttempts { get; init; } = 5;

    /// <summary>
    /// Gets the username-wide failure ceiling across addresses. Values at or below the address limit are treated as that limit plus one.
    /// </summary>
    public int MaxFailedAttemptsPerUsername { get; init; } = 50;

    /// <summary>
    /// Gets the failure window in seconds.
    /// </summary>
    public int WindowSeconds { get; init; } = 300;

    /// <summary>
    /// Gets the lockout duration in seconds.
    /// </summary>
    public int LockoutSeconds { get; init; } = 300;

    /// <summary>
    /// Gets the maximum counters in each of the username and username/address tables.
    /// </summary>
    public int MaxTrackedEntries { get; init; } = 10000;
}

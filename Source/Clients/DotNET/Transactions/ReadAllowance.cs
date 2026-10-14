// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Identifies an explicit allowance for Chronicle reads, never immediate appends.
/// </summary>
public enum ReadAllowance
{
    /// <summary>
    /// Reads whose potentially stale values are advisory.
    /// </summary>
    Advisory = 0,

    /// <summary>
    /// Reads enrolled atomically before their values are exposed.
    /// </summary>
    Contributed = 1
}

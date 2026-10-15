// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Monotone intent retained when semantically equal conditions are deduplicated.
/// </summary>
[Flags]
internal enum ReadProtectionContributionIntent
{
    /// <summary>
    /// No explicit validation request.
    /// </summary>
    None = 0,

    /// <summary>
    /// Acquired while reading, not a validate-only request.
    /// </summary>
    ReadEnrolled = 1,

    /// <summary>
    /// The caller requested validation even without events.
    /// </summary>
    ValidateOnly = 2
}

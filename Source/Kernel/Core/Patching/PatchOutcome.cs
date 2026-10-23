// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Describes the outcome of requesting patch application.
/// </summary>
public enum PatchOutcome
{
    /// <summary>
    /// Patching completed or was skipped because a newer kernel has already patched.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// Retry on a kernel at least as new as the caller.
    /// </summary>
    Relocate = 1
}

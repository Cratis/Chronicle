// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Defines the patch manager grain.
/// </summary>
/// <remarks>
/// Raised without a new method so an older activation rejects this release's calls and Orleans
/// re-places the grain on a silo at least this new.
/// </remarks>
[Version(2)]
public interface IPatchManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Apply all pending patches.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ApplyPatches();

    /// <summary>
    /// Applies patches on a kernel at least as new as the caller.
    /// </summary>
    /// <param name="caller">The calling kernel version.</param>
    /// <returns>The outcome, including whether the grain must relocate.</returns>
    Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller);
}

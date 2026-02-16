// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;

namespace Cratis.Chronicle.Grains.Patching;

/// <summary>
/// Represents the state for the <see cref="PatchManager"/>.
/// </summary>
/// <param name="AppliedPatches">Collection of patches that have been applied.</param>
public record PatchManagerState(IEnumerable<Patch> AppliedPatches)
{
    /// <summary>
    /// Gets an empty state.
    /// </summary>
    public static readonly PatchManagerState Empty = new([]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Patching;

/// <summary>
/// Defines the patch manager grain.
/// </summary>
public interface IPatchManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Apply all pending patches.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ApplyPatches();
}

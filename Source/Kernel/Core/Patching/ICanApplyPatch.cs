// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Defines a system that can apply a patch.
/// </summary>
public interface ICanApplyPatch
{
    /// <summary>
    /// Gets the version this patch applies to.
    /// </summary>
    SemanticVersion Version { get; }

    /// <summary>
    /// Gets the name of the patch (automatically derived from type name).
    /// </summary>
    string Name => GetType().Name;

    /// <summary>
    /// Apply the patch (upgrade scenario).
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Up();

    /// <summary>
    /// Revert the patch (downgrade scenario).
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Down();
}

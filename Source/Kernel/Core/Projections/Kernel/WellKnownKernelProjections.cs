// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Holds the naming convention for projections the kernel owns.
/// </summary>
public static class WellKnownKernelProjections
{
    /// <summary>
    /// The prefix every kernel-owned projection identifier carries.
    /// </summary>
    /// <remarks>
    /// The same prefix kernel reactors ("$system.{reactorId}") and pattern capture
    /// ("$system.patterns") already use, so anything kernel-owned reads the same way wherever it surfaces.
    /// </remarks>
    public const string Prefix = "$system.";

    /// <summary>
    /// Build the full <see cref="ProjectionId"/> for a system projection.
    /// </summary>
    /// <param name="id">The identifier without the prefix.</param>
    /// <returns>The prefixed <see cref="ProjectionId"/>.</returns>
    public static ProjectionId IdentifierFor(string id) => new($"{Prefix}{id}");

    /// <summary>
    /// Check whether a <see cref="ProjectionId"/> names a kernel-owned projection.
    /// </summary>
    /// <param name="identifier">The <see cref="ProjectionId"/> to check.</param>
    /// <returns>True if it carries the system prefix, false otherwise.</returns>
    /// <remarks>
    /// This is for display and diagnostics. A guard that has to refuse an operation asks
    /// <see cref="Concepts.Projections.Definitions.ProjectionDefinition.IsKernelOwned"/> instead, so ownership is never inferred from a string someone could
    /// produce by naming a projection carefully.
    /// </remarks>
    public static bool IsSystem(ProjectionId identifier) => identifier.Value.StartsWith(Prefix, StringComparison.Ordinal);
}

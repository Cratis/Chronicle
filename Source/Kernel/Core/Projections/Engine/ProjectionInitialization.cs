// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Determines when child content can initialize a root that has no creation clause of its own.
/// </summary>
internal static class ProjectionInitialization
{
    /// <summary>
    /// Checks whether a projection's roots have no creation clause of their own, so that only child content creates them.
    /// </summary>
    /// <param name="projection">The projection to check.</param>
    /// <returns>True if the projection is child-only; otherwise false.</returns>
    /// <remarks>
    /// This is a definition-level check. Reads use it to decide whether roots stored as uninitialized are visible.
    /// </remarks>
    internal static bool IsChildOnly(IProjection projection) =>
        !projection.HasParent && !projection.SubscribesToAllEvents && projection.ChildProjections.Any() &&
        !projection.OperationTypes.Values.Any(operation => operation.HasFlag(ProjectionOperationType.From) && !operation.HasFlag(ProjectionOperationType.Join));

    /// <summary>
    /// Checks whether a constructive child event can initialize a root without an own creator registration.
    /// </summary>
    /// <param name="projection">The projection being initialized.</param>
    /// <param name="context">The event being processed.</param>
    /// <returns>Whether the child event can initialize the root.</returns>
    internal static bool CanInitializeFromChildren(IProjection projection, ProjectionEventContext context) =>
        IsChildOnly(projection) &&
        context.ChildrenAffected && !context.IsJoin && context.OperationType.HasFlag(ProjectionOperationType.From);
}

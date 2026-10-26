// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Determines when child content can initialize a root that has no creation clause of its own.
/// </summary>
internal static class ProjectionInitialization
{
    /// <summary>
    /// Checks whether a constructive child event can initialize a root without an own creator registration.
    /// </summary>
    /// <param name="projection">The projection being initialized.</param>
    /// <param name="context">The event being processed.</param>
    /// <returns>Whether the child event can initialize the root.</returns>
    internal static bool CanInitializeFromChildren(IProjection projection, ProjectionEventContext context) =>
        !projection.HasParent && !projection.SubscribesToAllEvents && projection.ChildProjections.Any() &&
        context.ChildrenAffected && !context.IsJoin && context.OperationType.HasFlag(ProjectionOperationType.From) &&
        !projection.OperationTypes.Values.Any(operation => operation.HasFlag(ProjectionOperationType.From) && !operation.HasFlag(ProjectionOperationType.Join));
}

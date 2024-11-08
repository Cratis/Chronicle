// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections;

public static class ProjectionExtensions
{
    public static bool IsJoin(this IProjection projection, AppendedEvent appendedEvent) =>
        projection.OperationTypes.TryGetValue(appendedEvent.Metadata.Type, out var operationType) &&
        operationType.HasFlag(ProjectionOperationType.Join);
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Exception that gets thrown when an instance is not possible to resolve.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToResolveInstanceForProjection"/> class.
/// </remarks>
/// <param name="projectionPath">Path within the projection.</param>
public class UnableToResolveInstanceForProjection(ProjectionPath projectionPath) : Exception($"Projection with path '{projectionPath.Value}' can't resolve the instance to project to.")
{
}

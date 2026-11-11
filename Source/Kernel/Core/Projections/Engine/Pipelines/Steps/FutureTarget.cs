// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents a <see cref="ProjectionFuture"/> being resolved, with the projections it is resolved against.
/// </summary>
/// <param name="Projection">The root <see cref="IProjection"/> the step runs for.</param>
/// <param name="ChildProjection">The child <see cref="IProjection"/> the future belongs to.</param>
/// <param name="Future">The <see cref="ProjectionFuture"/> being resolved.</param>
internal readonly record struct FutureTarget(IProjection Projection, IProjection ChildProjection, ProjectionFuture Future);

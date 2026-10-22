// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// The exception that is thrown when a materialization generation is requested for a projection that is not namespaced.
/// </summary>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection.</param>
/// <param name="scope">The <see cref="ProjectionScope"/> of the projection.</param>
public class GlobalProjectionCannotBuildGenerations(ProjectionId projectionId, ProjectionScope scope)
    : Exception($"Projection '{projectionId.Value}' has scope '{scope}' and cannot be built as a materialization generation. Only namespaced projections can.");

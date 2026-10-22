// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// The exception that is thrown when a materialization generation is requested for a projection whose sink cannot be built privately.
/// </summary>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection.</param>
/// <param name="sinkType">The <see cref="SinkTypeId"/> of the sink.</param>
public class SinkCannotBuildGenerations(ProjectionId projectionId, SinkTypeId sinkType)
    : Exception($"Projection '{projectionId.Value}' writes to sink '{sinkType.Value}', which cannot be built privately as a materialization generation.");

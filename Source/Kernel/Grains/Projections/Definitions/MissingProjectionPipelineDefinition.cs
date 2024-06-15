// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;
using Cratis.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections.Definitions;

/// <summary>
/// Exception that gets thrown when a <see cref="ProjectionPipelineDefinition"/> is missing in the system.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingProjectionPipelineDefinition"/> class.
/// </remarks>
/// <param name="identifier"><see cref="ProjectionId"/> of the missing identifier.</param>
public class MissingProjectionPipelineDefinition(ProjectionId identifier) : Exception($"Missing projection pipeline definition for projection with id '{identifier}'")
{
}

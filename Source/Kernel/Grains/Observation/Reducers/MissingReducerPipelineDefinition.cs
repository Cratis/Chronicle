// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation.Reducers;

namespace Cratis.Kernel.Grains.Observation.Reducers;

/// <summary>
/// Exception that gets thrown when a reducer pipeline definition is missing.
/// </summary>
public class MissingReducerPipelineDefinition : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingReducerPipelineDefinition"/> class.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> that has a missing definition.</param>
    public MissingReducerPipelineDefinition(ReducerId reducerId) : base($"Missing reducer pipeline definition for reducer with id '{reducerId}'")
    {
    }
}

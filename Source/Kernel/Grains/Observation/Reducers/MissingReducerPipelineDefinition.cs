// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Exception that gets thrown when a reducer pipeline definition is missing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingReducerPipelineDefinition"/> class.
/// </remarks>
/// <param name="reducerId"><see cref="ReducerId"/> that has a missing definition.</param>
public class MissingReducerPipelineDefinition(ReducerId reducerId)
    : Exception($"Missing reducer pipeline definition for reducer with id '{reducerId}'");

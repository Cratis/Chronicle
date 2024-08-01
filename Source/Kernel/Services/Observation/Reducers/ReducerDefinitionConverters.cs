// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// Extension methods for converting between <see cref="ReducerDefinition"/> and <see cref="Contracts.Observation.Reducers.ReducerDefinition"/>.
/// </summary>
public static class ReducerDefinitionConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Observation.Reducers.ReducerDefinition"/> to <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="reducerDefinition"><see cref="Contracts.Observation.Reducers.ReducerDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="ReducerDefinition"/>.</returns>
    public static ReducerDefinition ToChronicle(this Contracts.Observation.Reducers.ReducerDefinition reducerDefinition) =>
        new(
            reducerDefinition.ReducerId,
            reducerDefinition.EventSequenceId,
            reducerDefinition.EventTypes.Select(_ => _.ToChronicle()),
            reducerDefinition.Model.ToChronicle(),
            reducerDefinition.Sink.ToChronicle()
        );
}

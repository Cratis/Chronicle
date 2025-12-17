// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Services.Observation.Reactors;

/// <summary>
/// Extension methods for converting between <see cref="ReactorDefinition"/> and <see cref="Contracts.Observation.Reactors.ReactorDefinition"/>.
/// </summary>
internal static class ReactorDefinitionConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Observation.Reactors.ReactorDefinition"/> to <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="reactorDefinition"><see cref="Contracts.Observation.Reactors.ReactorDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="ReactorDefinition"/>.</returns>
    public static ReactorDefinition ToChronicle(this Contracts.Observation.Reactors.ReactorDefinition reactorDefinition) =>
        new(
            reactorDefinition.ReactorId,
            ReactorOwner.Client,
            reactorDefinition.EventSequenceId,
            reactorDefinition.EventTypes.Select(_ => _.ToChronicle()),
            reactorDefinition.IsReplayable,
            reactorDefinition.Categories);
}

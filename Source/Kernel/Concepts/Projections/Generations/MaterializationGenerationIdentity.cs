// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the explicit binding between a generation's observer, the projection and generation it belongs to, and its physical destination.
/// </summary>
/// <remarks>
/// The observer identifier of a generation is never parsed back into a projection and generation; this binding is the
/// only way to go from one to the other. Generation 1 adopts the projection identifier as its observer identifier and the
/// existing container as its destination, so existing observer state, failed partitions and job requests stay valid.
/// Every later generation gets a distinct observer identifier and a distinct destination that is never renamed or reused.
/// </remarks>
/// <param name="ProjectionId">The <see cref="ProjectionId"/> the generation materializes.</param>
/// <param name="Generation">The <see cref="MaterializationGeneration"/>.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the generation's observer.</param>
/// <param name="Destination">The <see cref="ReadModelContainerName"/> the generation writes to and only it writes to.</param>
public record MaterializationGenerationIdentity(
    ProjectionId ProjectionId,
    MaterializationGeneration Generation,
    ObserverId ObserverId,
    ReadModelContainerName Destination)
{
    /// <summary>
    /// The separator between a projection identifier and the generation in a derived observer identifier.
    /// </summary>
    public const string ObserverIdSeparator = "@g";

    /// <summary>
    /// The separator between a container name and the generation in a derived destination.
    /// </summary>
    public const string DestinationSeparator = "-g";

    /// <summary>
    /// Derive the identity a generation is reserved under.
    /// </summary>
    /// <param name="projectionId">The <see cref="ProjectionId"/> the generation materializes.</param>
    /// <param name="generation">The <see cref="MaterializationGeneration"/> to derive for.</param>
    /// <param name="container">The <see cref="ReadModelContainerName"/> of the read model the projection materializes.</param>
    /// <returns>The derived <see cref="MaterializationGenerationIdentity"/>.</returns>
    /// <exception cref="InvalidMaterializationGeneration">Thrown when the generation is before the first generation.</exception>
    /// <remarks>
    /// Deriving is formatting only - a derived identity is not valid until it is reserved durably and collision-checked.
    /// </remarks>
    public static MaterializationGenerationIdentity For(ProjectionId projectionId, MaterializationGeneration generation, ReadModelContainerName container)
    {
        if (generation.Value < MaterializationGeneration.First.Value)
        {
            throw new InvalidMaterializationGeneration(projectionId, generation);
        }

        return generation.IsFirst
            ? new(projectionId, generation, projectionId, container)
            : new(
                projectionId,
                generation,
                new ObserverId($"{projectionId.Value}{ObserverIdSeparator}{generation.Value}"),
                new ReadModelContainerName($"{container.Value}{DestinationSeparator}{generation.Value}"));
    }
}

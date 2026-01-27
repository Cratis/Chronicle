// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a request to save a projection from its projection declaration language representation.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The projection declaration language representation of the projection.</param>
/// <param name="DraftReadModel">Optional draft read model definition to save along with the projection.</param>
[Command]
public record SaveProjection(string EventStore, string Namespace, string Declaration, DraftReadModel? DraftReadModel = null)
{
    /// <summary>
    /// Handles the save projection request.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>Result of saving the projection.</returns>
    internal async Task<SaveProjectionResult> Handle(IProjections projections)
    {
        var request = new SaveProjectionRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Declaration = Declaration,
            DraftReadModel = DraftReadModel is not null
                ? new DraftReadModelDefinition
                {
                    Name = DraftReadModel.Name,
                    Schema = DraftReadModel.Schema
                }
                : null
        };

        return await projections.Save(request);
    }
}

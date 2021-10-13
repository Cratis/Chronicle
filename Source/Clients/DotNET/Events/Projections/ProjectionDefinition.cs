// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the definition of a projection.
    /// </summary>
    /// <param name="Identifier"><see cref="ProjectionId">Identifier</see> of the projection.</param>
    /// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
    /// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
    public record ProjectionDefinition(
        ProjectionId Identifier,
        ModelDefinition Model,
        IDictionary<string, FromDefinition> From);
}

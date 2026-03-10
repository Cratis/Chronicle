// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a request to save a projection and simultaneously create its read model type
/// by inferring the schema from the event types used in the projection declaration.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The projection declaration language representation of the projection.</param>
/// <param name="ReadModelDisplayName">The display name for the read model type to be created.</param>
[Command]
public record SaveProjectionWithInferredReadModel(
    string EventStore,
    string Namespace,
    string Declaration,
    string ReadModelDisplayName)
{
    /// <summary>
    /// Handles the save request by building a draft read model definition from the display name
    /// and delegating to the projection service, which infers the schema from the event types.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>Collection of syntax errors, if any.</returns>
    internal async Task<IEnumerable<ProjectionDeclarationSyntaxError>> Handle(IProjections projections)
    {
        var identifier = ToIdentifier();
        var result = await projections.Save(new SaveProjectionRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Declaration = Declaration,
            DraftReadModel = new DraftReadModelDefinition
            {
                Identifier = identifier,
                DisplayName = ReadModelDisplayName,
                ContainerName = identifier,
                Schema = string.Empty
            }
        });

        return result.Errors.ToApi();
    }

    /// <summary>
    /// Derives a camelCase identifier from the read model display name.
    /// </summary>
    /// <returns>A camelCase identifier suitable for use as a read model identifier.</returns>
    internal string ToIdentifier()
    {
        var words = ReadModelDisplayName.Trim().Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(words.Select((word, i) =>
            i == 0
                ? char.ToLowerInvariant(word[0]) + word[1..]
                : char.ToUpperInvariant(word[0]) + word[1..]));
    }
}

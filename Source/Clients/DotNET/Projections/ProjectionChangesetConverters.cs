// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Converters for <see cref="ProjectionChangeset{T}"/>.
/// </summary>
internal static class ProjectionChangesetConverters
{
    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="changeset">Contract representation of changeset.</param>
    /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
    /// <typeparam name="TReadModel">Type of read model it should convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionChangeset{T}"/>.</returns>
    internal static ProjectionChangeset<TReadModel> ToClient<TReadModel>(this Contracts.Projections.ProjectionChangeset changeset, JsonSerializerOptions jsonSerializerOptions) => new(
            changeset.Namespace,
            changeset.ReadModelKey,
            changeset.Removed ? default : JsonSerializer.Deserialize<TReadModel>(changeset.ReadModel, jsonSerializerOptions),
            changeset.Removed);
}

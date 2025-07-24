// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;

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
    /// <typeparam name="TReadModel">Type of read model it should convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionChangeset{T}"/>.</returns>
    internal static ProjectionChangeset<TReadModel> ToClient<TReadModel>(this Contracts.Projections.ProjectionChangeset changeset) => new(
            changeset.Namespace,
            changeset.ModelKey,
            JsonSerializer.Deserialize<TReadModel>(changeset.Model, Globals.JsonSerializerOptions)!);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Converters for <see cref="ProjectionChangeset{T}"/>.
/// </summary>
public static class ProjectionChangesetConverters
{
    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="changeset">Contract representation of changeset.</param>
    /// <typeparam name="TModel">Type of model it should convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionChangeset{T}"/>.</returns>
    public static ProjectionChangeset<TModel> ToClient<TModel>(this Contracts.Projections.ProjectionChangeset changeset) => new(
            changeset.Namespace,
            changeset.ModelKey,
            JsonSerializer.Deserialize<TModel>(changeset.Model, Globals.JsonSerializerOptions)!);
}

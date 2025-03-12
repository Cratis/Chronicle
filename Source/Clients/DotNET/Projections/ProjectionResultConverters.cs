// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extension methods for converting from <see cref="Contracts.Projections.ProjectionResult"/>.
/// </summary>
internal static class ProjectionResultConverters
{
    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <typeparam name="TModel">Type of model to convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult<TModel> ToClient<TModel>(this Contracts.Projections.ProjectionResult result) =>
        new(
            JsonSerializer.Deserialize<TModel>(result.Model, Globals.JsonSerializerOptions)!,
            result.AffectedProperties.Select(_ => (PropertyPath)_),
            result.ProjectedEventsCount);

    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <param name="modelType">Type of model to convert to.</param>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult ToClient(this Contracts.Projections.ProjectionResult result, Type modelType) =>
        new(
            JsonSerializer.Deserialize(result.Model, modelType, Globals.JsonSerializerOptions)!,
            result.AffectedProperties.Select(_ => (PropertyPath)_),
            result.ProjectedEventsCount);

    /// <summary>
    /// Convert to the raw client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResultRaw ToClient(this Contracts.Projections.ProjectionResult result) =>
        new(
            (JsonObject)JsonNode.Parse(result.Model)!,
            result.AffectedProperties.Select(_ => (PropertyPath)_),
            result.ProjectedEventsCount);
}

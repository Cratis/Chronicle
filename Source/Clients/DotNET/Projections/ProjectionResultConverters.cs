// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

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
    /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
    /// <typeparam name="TReadModel">Type of read model to convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult<TReadModel> ToClient<TReadModel>(this Contracts.Projections.ProjectionResult result, JsonSerializerOptions jsonSerializerOptions) =>
        new(
            JsonSerializer.Deserialize<TReadModel>(result.ReadModel, jsonSerializerOptions)!,
            result.ProjectedEventsCount,
            result.LastHandledEventSequenceNumber);

    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <param name="readModelType">Type of read model to convert to.</param>
    /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult ToClient(this Contracts.Projections.ProjectionResult result, Type readModelType, JsonSerializerOptions jsonSerializerOptions) =>
        new(
            JsonSerializer.Deserialize(result.ReadModel, readModelType, jsonSerializerOptions)!,
            result.ProjectedEventsCount,
            result.LastHandledEventSequenceNumber);

    /// <summary>
    /// Convert to the raw client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResultRaw ToClient(this Contracts.Projections.ProjectionResult result) =>
        new(
            (JsonObject)JsonNode.Parse(result.ReadModel)!,
            result.ProjectedEventsCount,
            result.LastHandledEventSequenceNumber);
}

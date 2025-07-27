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
    /// <typeparam name="TReadModel">Type of read model to convert to.</typeparam>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult<TReadModel> ToClient<TReadModel>(this Contracts.Projections.ProjectionResult result) =>
        new(
            JsonSerializer.Deserialize<TReadModel>(result.ReadModel, Globals.JsonSerializerOptions)!,
            result.AffectedProperties.Select(_ => (PropertyPath)_),
            result.ProjectedEventsCount,
            result.LastHandledEventSequenceNumber);

    /// <summary>
    /// Convert to client representation.
    /// </summary>
    /// <param name="result"><see cref="Contracts.Projections.ProjectionResult"/> to convert from.</param>
    /// <param name="readModelType">Type of read model to convert to.</param>
    /// <returns>Converted <see cref="ProjectionResult"/>.</returns>
    internal static ProjectionResult ToClient(this Contracts.Projections.ProjectionResult result, Type readModelType) =>
        new(
            JsonSerializer.Deserialize(result.ReadModel, readModelType, Globals.JsonSerializerOptions)!,
            result.AffectedProperties.Select(_ => (PropertyPath)_),
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
            result.AffectedProperties.Select(_ => (PropertyPath)_),
            result.ProjectedEventsCount,
            result.LastHandledEventSequenceNumber);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a request to preview a projection.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Dsl">The DSL representation of the projection.</param>
[Command]
public record PreviewProjection(string EventStore, string Namespace, string Dsl)
{
    /// <summary>
    /// Handles the preview projection request.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>The projection preview.</returns>
    internal async Task<ProjectionPreview> Handle(IProjections projections)
    {
        var request = new PreviewProjectionRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Dsl = Dsl
        };

        var result = await projections.PreviewFromDsl(request);

        return result.Value switch
        {
            Contracts.Projections.ProjectionPreview preview => new ProjectionPreview(
                preview.ReadModelEntries.Select(s => JsonNode.Parse(s)?.AsObject() ?? new JsonObject()),
                JsonNode.Parse(preview.ReadModel.Schema)!.AsObject(),
                []),

            ProjectionDefinitionParsingErrors errors => new ProjectionPreview(
                [],
                new JsonObject(),
                errors.Errors.Select(e => new ProjectionDefinitionSyntaxError(e.Message, e.Line, e.Column))),

            _ => throw new InvalidOperationException("Unexpected result type from PreviewFromDsl")
        };
    }
}

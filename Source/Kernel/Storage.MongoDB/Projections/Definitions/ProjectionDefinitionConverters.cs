// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters between MongoDB ProjectionDefinition and Kernel ProjectionDefinition.
/// </summary>
public static class ProjectionDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB ProjectionDefinition to a Kernel ProjectionDefinition.
    /// </summary>
    /// <param name="source">The MongoDB ProjectionDefinition to convert.</param>
    /// <param name="sink">The sink definition to use.</param>
    /// <returns>A Kernel ProjectionDefinition.</returns>
    public static KernelDefs.ProjectionDefinition ToKernel(this ProjectionDefinition source, SinkDefinition sink) =>
        new(
            source.Owner,
            source.EventSequenceId,
            source.Identifier,
            source.ReadModel,
            source.IsActive,
            source.IsRewindable,
            JsonNode.Parse(source.InitialModelState.ToJson()) as JsonObject ?? new JsonObject(),
            source.From.ToDictionary(
                kv => EventType.Parse(kv.Key).ToKernel(),
                kv => kv.Value.ToKernel()),
            source.Join.ToDictionary(
                kv => EventType.Parse(kv.Key).ToKernel(),
                kv => kv.Value.ToKernel()),
            source.Children.ToDictionary(
                kv => (PropertyPath)kv.Key,
                kv => kv.Value.ToKernel()),
            source.FromDerivatives.Select(fd => fd.ToKernel()),
            source.FromEvery.ToKernel(),
            sink,
            source.RemovedWith.ToDictionary(
                kv => EventType.Parse(kv.Key).ToKernel(),
                kv => kv.Value.ToKernel()),
            source.RemovedWithJoin.ToDictionary(
                kv => EventType.Parse(kv.Key).ToKernel(),
                kv => kv.Value.ToKernel()),
            source.FromEventProperty?.ToKernel(),
            source.LastUpdated,
            source.Categories
        );

    /// <summary>
    /// Converts a Kernel ProjectionDefinition to a MongoDB ProjectionDefinition.
    /// </summary>
    /// <param name="source">The Kernel ProjectionDefinition to convert.</param>
    /// <returns>A MongoDB ProjectionDefinition.</returns>
    public static ProjectionDefinition ToMongoDB(this KernelDefs.ProjectionDefinition source) =>
        new()
        {
            Owner = source.Owner,
            EventSequenceId = source.EventSequenceId,
            Identifier = source.Identifier,
            ReadModel = source.ReadModel,
            IsActive = source.IsActive,
            IsRewindable = source.IsRewindable,
            InitialModelState = BsonDocument.Parse(source.InitialModelState.ToJsonString()),
            From = source.From.ToDictionary(
                kv => kv.Key.ToMongoDB().ToString(),
                kv => kv.Value.ToMongoDB()),
            Join = source.Join.ToDictionary(
                kv => kv.Key.ToMongoDB().ToString(),
                kv => kv.Value.ToMongoDB()),
            Children = source.Children.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value.ToMongoDB()),
            FromDerivatives = source.FromDerivatives.Select(fd => fd.ToMongoDB()),
            FromEvery = source.FromEvery.ToMongoDB(),
            RemovedWith = source.RemovedWith.ToDictionary(
                kv => kv.Key.ToMongoDB().ToString(),
                kv => kv.Value.ToMongoDB()),
            RemovedWithJoin = source.RemovedWithJoin.ToDictionary(
                kv => kv.Key.ToMongoDB().ToString(),
                kv => kv.Value.ToMongoDB()),
            FromEventProperty = source.FromEventProperty?.ToMongoDB(),
            LastUpdated = source.LastUpdated,
            Categories = source.Categories ?? []
        };
}

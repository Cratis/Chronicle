// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represent an implementation of <see cref="IProjectionDefinitionComparer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class ProjectionDefinitionComparer(IStorage storage, IObjectComparer objectComparer, ILogger<ProjectionDefinitionComparer> logger) : IProjectionDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<ProjectionDefinitionCompareResult> Compare(
        ProjectionKey projectionKey,
        ProjectionDefinition first,
        ProjectionDefinition second)
    {
        if (!await storage.GetEventStore(projectionKey.EventStore).Projections.Has(projectionKey.ProjectionId))
        {
            logger.ProjectionIsNew(projectionKey.ProjectionId);
            return ProjectionDefinitionCompareResult.New;
        }

        logger.ComparingDefinitions(projectionKey.ProjectionId);

        // Note: Ignore the model and initial model state as they are not relevant for comparison and also have potential for recursive comparison
        // that can potentially lead to a stack overflow. LastUpdated is also ignored since it is metadata, not structural definition.
        // Optional collections are normalized to avoid false positives caused by storage serialization defaults (null vs empty).
        first = Normalize(first);
        second = Normalize(second);

        return objectComparer.Compare(first, second, ObjectComparerMode.Loose, out _)
            ? ProjectionDefinitionCompareResult.Same
            : ProjectionDefinitionCompareResult.Different;
    }

    static ProjectionDefinition Normalize(ProjectionDefinition definition) =>
        definition with
        {
            ReadModel = null!,
            InitialModelState = null!,
            LastUpdated = null,
            Tags = NormalizeTags(definition.Tags),
            Nested = NormalizeNested(definition.Nested)
        };

    static string[]? NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return null;
        }

        var normalizedTags = tags.ToArray();
        return normalizedTags.Length == 0 ? null : normalizedTags.Order().ToArray();
    }

    static IDictionary<PropertyPath, ChildrenDefinition>? NormalizeNested(IDictionary<PropertyPath, ChildrenDefinition>? nested)
    {
        if (nested is null || nested.Count == 0)
        {
            return null;
        }

        return nested;
    }
}

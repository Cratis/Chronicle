// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Normalizes storage defaults without changing known projection rules.
/// </summary>
internal static class ProjectionDefinitionNormalization
{
    /// <summary>
    /// Normalizes a definition for comparison, treating rules older storage never retained as unknown.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> to normalize.</param>
    /// <param name="hasLegacyStorageFields">Whether the definition was read from a legacy stored document.</param>
    /// <returns>The normalized <see cref="ProjectionDefinition"/>.</returns>
    internal static ProjectionDefinition ForComparison(ProjectionDefinition definition, bool hasLegacyStorageFields)
    {
        // Older MongoDB converters never retained these three rules. Unknown is not a change;
        // all other rules are still compared, and lossless definitions never take this path.
        var fromEvery = hasLegacyStorageFields
            ? definition.FromEvery with { Key = Concepts.WellKnownExpressions.EventSourceId, AutoMap = AutoMap.Inherit }
            : definition.FromEvery;
        var normalized = definition with
        {
            ReadModel = null!,
            InitialModelState = null!,
            LastUpdated = null,
            Tags = null,
            Version = null,
            HasLegacyStorageFields = false,
            Scope = hasLegacyStorageFields ? ProjectionScope.Namespaced : definition.Scope,
            FromEvery = fromEvery,
            Children = NormalizeChildren(definition.Children, hasLegacyStorageFields),
            Nested = definition.Nested is { Count: > 0 } ? NormalizeChildren(definition.Nested, hasLegacyStorageFields) : null,
            NoAutoMapProperties = definition.NoAutoMapProperties ?? []
        };
        return normalized is ChildrenDefinition child ? child with { All = fromEvery } : normalized;
    }

    /// <summary>
    /// Keeps the rules of an authoritative lossless definition when the incoming one was read from legacy storage.
    /// </summary>
    /// <param name="incoming">The incoming <see cref="ProjectionDefinition"/>.</param>
    /// <param name="authoritative">The authoritative <see cref="ProjectionDefinition"/> currently held.</param>
    /// <returns>The <see cref="ProjectionDefinition"/> to use.</returns>
    internal static ProjectionDefinition PreserveKnownStorageFields(ProjectionDefinition incoming, ProjectionDefinition authoritative)
    {
        if (!incoming.HasLegacyStorageFields || authoritative.HasLegacyStorageFields || authoritative.FromEvery is null)
        {
            return incoming;
        }

        return RestoreKnownFields(incoming, authoritative) with { HasLegacyStorageFields = false };
    }

    static ProjectionDefinition RestoreKnownFields(ProjectionDefinition incoming, ProjectionDefinition authoritative)
    {
        var fromEvery = incoming.FromEvery with { Key = authoritative.FromEvery.Key, AutoMap = authoritative.FromEvery.AutoMap };
        var restored = incoming with
        {
            Scope = authoritative.Scope,
            FromEvery = fromEvery,
            Children = RestoreChildren(incoming.Children, authoritative.Children),
            Nested = incoming.Nested is null ? null : RestoreChildren(incoming.Nested, authoritative.Nested)
        };
        return restored is ChildrenDefinition child ? child with { All = fromEvery } : restored;
    }

    static Dictionary<PropertyPath, ChildrenDefinition> NormalizeChildren(IDictionary<PropertyPath, ChildrenDefinition> children, bool hasLegacyStorageFields) =>
        children.ToDictionary(pair => pair.Key, pair => (ChildrenDefinition)ForComparison(pair.Value, hasLegacyStorageFields));

    static Dictionary<PropertyPath, ChildrenDefinition> RestoreChildren(
        IDictionary<PropertyPath, ChildrenDefinition> incoming,
        IDictionary<PropertyPath, ChildrenDefinition>? authoritative) =>
        incoming.ToDictionary(
            pair => pair.Key,
            pair => authoritative is not null && authoritative.TryGetValue(pair.Key, out var known)
                ? (ChildrenDefinition)RestoreKnownFields(pair.Value, known)
                : pair.Value);
}

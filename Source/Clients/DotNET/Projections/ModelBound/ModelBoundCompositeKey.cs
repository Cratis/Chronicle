// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Builds a composite key whose parts have matching event and key member paths.
/// </summary>
static class ModelBoundCompositeKey
{
    /// <summary>
    /// Reads the optional composite key paths from a model-bound attribute.
    /// </summary>
    /// <param name="attribute">The attribute carrying the key declaration.</param>
    /// <returns>The declared paths, or null for a single key.</returns>
    internal static string[]? Read(Attribute attribute) =>
        attribute.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.CompositeKey))?.GetValue(attribute) as string[];

    /// <summary>
    /// Builds the same key expression as mapping each matching member with the fluent composite key builder.
    /// </summary>
    /// <param name="eventType">The event type supplying the key.</param>
    /// <param name="paths">The CLR paths of the key parts.</param>
    /// <param name="namingPolicy">The naming policy for the event and key members.</param>
    /// <returns>The serialized composite expression.</returns>
    /// <exception cref="ArgumentException">Fewer than two distinct, non-empty property paths were supplied.</exception>
    internal static string Build(Type eventType, string[] paths, INamingPolicy namingPolicy)
    {
        if (paths.Length < 2 || Array.Exists(paths, string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException($"A composite key for '{eventType.Name}' requires at least two non-empty event property paths.", nameof(paths));
        }

        var parts = paths.Select(path => ResolvePart(eventType, path, namingPolicy)).ToArray();
        if (parts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != parts.Length)
        {
            throw new ArgumentException($"A composite key for '{eventType.Name}' requires distinct event property paths.", nameof(paths));
        }

        return $"{WellKnownExpressions.Composite}({string.Join(',', parts.Select(part => $"{part}={part}"))})";
    }

    static string ResolvePart(Type eventType, string path, INamingPolicy namingPolicy) =>
        ProjectionPropertyPaths.NameOf(PropertyValidator.ValidatePropertyExists(eventType, path), namingPolicy);
}

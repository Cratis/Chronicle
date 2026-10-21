// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Identifies mappings that model-bound attributes cannot express without changing their meaning.
/// </summary>
static class ModelBoundLimitations
{
    /// <summary>
    /// Determines whether attributes can represent an every-event mapping.
    /// </summary>
    /// <param name="definition">The projection scope.</param>
    /// <param name="property">The mapping target.</param>
    /// <param name="expression">The stored expression.</param>
    /// <returns>Whether a faithful attribute mapping is available.</returns>
    internal static bool CanMapEvery(ProjectionDefinition definition, string property, string expression)
    {
        if (property.Contains($".{Concepts.WellKnownExpressions.EventContext}.", StringComparison.Ordinal))
        {
            return false;
        }
        var source = ProjectionExpressions.ReadValue(expression);
        return definition.Owner == ProjectionOwner.Parent
            ? source.Kind is ProjectionValueKind.EventContextProperty or ProjectionValueKind.EventSourceId
            : source.Kind is ProjectionValueKind.EventContextProperty or ProjectionValueKind.EventSourceId or ProjectionValueKind.EventProperty && IsPlainMapping(expression);
    }

    /// <summary>
    /// Explains unsupported mappings next to their target member.
    /// </summary>
    /// <param name="definition">The projection scope.</param>
    /// <param name="property">The target member.</param>
    /// <param name="scope">The human-readable scope name.</param>
    /// <returns>The generated C# comments.</returns>
    internal static IEnumerable<string> Comments(ProjectionDefinition definition, string property, string scope)
    {
        foreach (var mapping in definition.FromEvery.Properties.Where(mapping => mapping.Key.Path == property || mapping.Key.Path.StartsWith(property + ".", StringComparison.Ordinal)))
        {
            if (!CanMapEvery(definition, mapping.Key.Path, mapping.Value))
            {
                yield return $"// Not expressible with model-bound attributes: every-mapping '{mapping.Key.Path}' in {scope}. Use the declarative projection.";
                continue;
            }
            if (definition.Owner != ProjectionOwner.Parent)
            {
                continue;
            }
            foreach (var join in definition.Join.Keys.Where(join => !definition.From.ContainsKey(join)))
            {
                yield return $"// Not expressible with model-bound attributes: every-mapping '{mapping.Key.Path}' for join event '{join.Id.Value}' in {scope}. Use the declarative projection.";
            }
        }
    }

    static bool IsPlainMapping(string expression)
    {
        var normalized = DeclarativeExpressions.Normalize(expression);
        return normalized is not Concepts.WellKnownExpressions.Count and not Concepts.WellKnownExpressions.Increment and not Concepts.WellKnownExpressions.Decrement
            && !normalized.StartsWith($"{Concepts.WellKnownExpressions.Add}(", StringComparison.Ordinal)
            && !normalized.StartsWith($"{Concepts.WellKnownExpressions.Subtract}(", StringComparison.Ordinal);
    }
}

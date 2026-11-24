// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// Checks the root-only flat key contract for definitions, including definitions not compiled from PDL.
/// </summary>
internal static class FlatCompositeKeyDefinition
{
    /// <summary>
    /// Checks whether a root handler selects a flat key.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <returns>Whether any root handler selects a flat key.</returns>
    internal static bool UsesFlatKey(ProjectionDefinition definition) =>
        RootKeys(definition).Any(FlatCompositeKeyExpression.IsFlatComposite);

    /// <summary>
    /// Checks every scope for flat key expressions.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <returns>Whether the definition contains a flat key expression.</returns>
    internal static bool HasFlatKey(ProjectionDefinition definition) =>
        UsesFlatKey(definition) || HasFlatJoin(definition) || FlatCompositeKeyExpression.IsFlatComposite(definition.FromEvery.Key.Value) || Descendants(definition).Any(HasFlatKey);

    /// <summary>
    /// Refuses unsupported scopes and incomplete root key declarations.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <param name="readModel">The root read model.</param>
    /// <exception cref="FlatCompositeKeyNotSupported">The flat key definition violates the root tuple contract.</exception>
    internal static void Validate(ProjectionDefinition definition, ReadModelDefinition readModel)
    {
        if (Descendants(definition).Any(HasFlatKey) || HasFlatJoin(definition) || FlatCompositeKeyExpression.IsFlatComposite(definition.FromEvery.Key.Value))
        {
            throw new FlatCompositeKeyNotSupported("Flat composite keys identify only the root read model.");
        }

        if (!UsesFlatKey(definition)) return;
        ValidateScope(definition);

        ValidateProperties(readModel, ValidateRootKeys(definition));
    }

    /// <summary>
    /// Checks the scalar schema shapes supported by flat key parts.
    /// </summary>
    /// <param name="property">The key part's property schema.</param>
    /// <returns>Whether the property has a supported scalar type.</returns>
    internal static bool IsScalarProperty(JsonSchema property) =>
        (property.ActualSchema.Type & ~JsonObjectType.Null) is JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean;

    /// <summary>
    /// Checks for protection metadata on a property or its references.
    /// </summary>
    /// <param name="property">The key part's property schema.</param>
    /// <returns>Whether using this property in an identity would bypass protection.</returns>
    internal static bool IsProtectedProperty(JsonSchema property) => property.HasComplianceMetadata() || property.HasSecurityMetadata();

    static string[] ValidateRootKeys(ProjectionDefinition definition)
    {
        string[]? expected = null;
        foreach (var expression in RootKeys(definition))
        {
            if (!FlatCompositeKeyExpression.IsFlatComposite(expression))
            {
                throw new FlatCompositeKeyNotSupported("Every root handler must supply the flat composite key parts.");
            }

            var names = FlatCompositeKeyExpression.Parse(expression).Parts.Select(part => part.Property).ToArray();
            if (expected is not null && !names.SequenceEqual(expected))
            {
                throw new FlatCompositeKeyNotSupported("Every root handler must supply the same flat composite key parts.");
            }

            expected = names;
        }

        return expected!;
    }

    static void ValidateScope(ProjectionDefinition definition)
    {
        if (HasUnsupportedScope(definition))
        {
            throw new FlatCompositeKeyNotSupported("Flat composite keys support only root from and remove with handlers.");
        }

        if (HasEveryHandler(definition))
        {
            throw new FlatCompositeKeyNotSupported("Flat composite keys do not support every/all handlers.");
        }

        if (HasImplicitHandlers(definition))
        {
            throw new FlatCompositeKeyNotSupported("Flat composite keys support only explicit root event handlers.");
        }

        if (HasParentKeys(definition))
        {
            throw new FlatCompositeKeyNotSupported("Flat composite keys do not support parent keys.");
        }
    }

    static bool HasUnsupportedScope(ProjectionDefinition definition) =>
        definition.Graph is not null || definition.Children.Count > 0 || definition.Nested?.Count > 0 || HasJoins(definition);

    static bool HasJoins(ProjectionDefinition definition) => definition.Join.Count > 0 || definition.RemovedWithJoin.Count > 0;

    static bool HasEveryHandler(ProjectionDefinition definition) =>
        definition.SubscribesToAllEvents || definition.FromEvery.Properties.Count > 0 || definition.FromEvery.Key.Value != WellKnownExpressions.EventSourceId;

    static bool HasImplicitHandlers(ProjectionDefinition definition) =>
        definition.FromDerivatives?.Any() == true || definition.FromEventProperty is not null;

    static bool HasParentKeys(ProjectionDefinition definition) =>
        definition.From.Values.Any(from => from.ParentKey is not null) || definition.RemovedWith.Values.Any(removed => removed.ParentKey is not null);

    static void ValidateProperties(ReadModelDefinition readModel, IEnumerable<string> names)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        foreach (var name in names)
        {
            if (!schema.Properties.TryGetValue(name, out var property))
            {
                throw new FlatCompositeKeyNotSupported($"Flat composite key part '{name}' is not a root read model property.");
            }

            if (IsProtectedProperty(property))
            {
                throw new FlatCompositeKeyNotSupported($"Flat composite key part '{name}' cannot carry compliance or security metadata.");
            }

            if (!IsScalarProperty(property))
            {
                throw new FlatCompositeKeyNotSupported($"Flat composite key part '{name}' must be scalar.");
            }
        }
    }

    static bool HasFlatJoin(ProjectionDefinition definition) =>
        definition.Join.Values.Any(join => FlatCompositeKeyExpression.IsFlatComposite(join.Key.Value)) ||
        definition.RemovedWithJoin.Values.Any(join => FlatCompositeKeyExpression.IsFlatComposite(join.Key.Value));

    static IEnumerable<ProjectionDefinition> Descendants(ProjectionDefinition definition) =>
        definition.Children.Values.Concat(definition.Nested?.Values ?? []);

    static IEnumerable<string> RootKeys(ProjectionDefinition definition) =>
        definition.From.Values.Select(from => from.Key.Value)
            .Concat(definition.RemovedWith.Values.Select(removed => removed.Key.Value))
            .Concat((definition.FromDerivatives ?? []).Select(derivative => derivative.From.Key.Value));
}

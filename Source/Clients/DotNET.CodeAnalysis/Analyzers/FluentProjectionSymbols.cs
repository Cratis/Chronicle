// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The Chronicle fluent-projection symbols used by the projection analyzers.
/// </summary>
/// <remarks>
/// Resolving these once from the compilation makes builder recognition depend on the public Chronicle type's
/// metadata identity. A consumer-defined interface with the same simple name must never activate a Chronicle
/// analyzer.
/// </remarks>
sealed class FluentProjectionSymbols
{
    const string Namespace = "Cratis.Chronicle.Projections";

    FluentProjectionSymbols(
        INamedTypeSymbol projectionBuilder,
        INamedTypeSymbol childrenBuilder,
        INamedTypeSymbol readModelPropertiesBuilder,
        INamedTypeSymbol joinBuilder,
        INamedTypeSymbol setBuilder,
        INamedTypeSymbol typedSetBuilder,
        INamedTypeSymbol addBuilder,
        INamedTypeSymbol subtractBuilder,
        INamedTypeSymbol addChildBuilder)
    {
        ProjectionBuilder = projectionBuilder;
        ChildrenBuilder = childrenBuilder;
        ReadModelPropertiesBuilder = readModelPropertiesBuilder;
        JoinBuilder = joinBuilder;
        SetBuilder = setBuilder;
        TypedSetBuilder = typedSetBuilder;
        AddBuilder = addBuilder;
        SubtractBuilder = subtractBuilder;
        AddChildBuilder = addChildBuilder;
    }

    /// <summary>Gets the <c>IProjectionBuilder&lt;,&gt;</c> definition.</summary>
    internal INamedTypeSymbol ProjectionBuilder { get; }

    /// <summary>Gets the <c>IChildrenBuilder&lt;,&gt;</c> definition.</summary>
    internal INamedTypeSymbol ChildrenBuilder { get; }

    /// <summary>Gets the <c>IReadModelPropertiesBuilder&lt;,,&gt;</c> definition.</summary>
    internal INamedTypeSymbol ReadModelPropertiesBuilder { get; }

    /// <summary>Gets the <c>IJoinBuilder&lt;,&gt;</c> definition.</summary>
    internal INamedTypeSymbol JoinBuilder { get; }

    /// <summary>Gets the untyped <c>ISetBuilder&lt;,,&gt;</c> definition.</summary>
    internal INamedTypeSymbol SetBuilder { get; }

    /// <summary>Gets the typed <c>ISetBuilder&lt;,,,&gt;</c> definition.</summary>
    internal INamedTypeSymbol TypedSetBuilder { get; }

    /// <summary>Gets the <c>IAddBuilder&lt;,,,&gt;</c> definition.</summary>
    internal INamedTypeSymbol AddBuilder { get; }

    /// <summary>Gets the <c>ISubtractBuilder&lt;,,,&gt;</c> definition.</summary>
    internal INamedTypeSymbol SubtractBuilder { get; }

    /// <summary>Gets the <c>IAddChildBuilder&lt;,&gt;</c> definition.</summary>
    internal INamedTypeSymbol AddChildBuilder { get; }

    /// <summary>
    /// Resolve the complete fluent builder surface required by the analyzers.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <returns>The symbols, or <see langword="null"/> when this is not a Chronicle consumer compilation.</returns>
    internal static FluentProjectionSymbols? TryCreate(Compilation compilation)
    {
        var projectionBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IProjectionBuilder`2");
        var childrenBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IChildrenBuilder`2");
        var readModelPropertiesBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IReadModelPropertiesBuilder`3");
        var joinBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IJoinBuilder`2");
        var setBuilder = compilation.GetTypeByMetadataName($"{Namespace}.ISetBuilder`3");
        var typedSetBuilder = compilation.GetTypeByMetadataName($"{Namespace}.ISetBuilder`4");
        var addBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IAddBuilder`4");
        var subtractBuilder = compilation.GetTypeByMetadataName($"{Namespace}.ISubtractBuilder`4");
        var addChildBuilder = compilation.GetTypeByMetadataName($"{Namespace}.IAddChildBuilder`2");

        return projectionBuilder is null ||
               childrenBuilder is null ||
               readModelPropertiesBuilder is null ||
               joinBuilder is null ||
               setBuilder is null ||
               typedSetBuilder is null ||
               addBuilder is null ||
               subtractBuilder is null ||
               addChildBuilder is null
            ? null
            : new(
                projectionBuilder,
                childrenBuilder,
                readModelPropertiesBuilder,
                joinBuilder,
                setBuilder,
                typedSetBuilder,
                addBuilder,
                subtractBuilder,
                addChildBuilder);
    }

    /// <summary>
    /// Determine whether a method belongs to a specific Chronicle builder definition.
    /// </summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="builderDefinition">The expected original builder definition.</param>
    /// <returns>True when the declaring type is the expected Chronicle builder.</returns>
    internal static bool IsMethodOn(IMethodSymbol method, INamedTypeSymbol builderDefinition) =>
        SymbolEqualityComparer.Default.Equals(method.ContainingType?.OriginalDefinition, builderDefinition);
}

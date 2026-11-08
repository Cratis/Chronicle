// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Mirrors how the client tells batch methods from per-event methods, so the analyzers enforce the same rules registration does.
/// </summary>
/// <remarks>
/// A method whose first parameter is an EventBatch is a batch method, whatever its name. Its event types are the type
/// arguments of its [Observes&lt;T&gt;] attributes. Every other handler method is per-event.
/// </remarks>
internal static class BatchObservers
{
    /// <summary>
    /// Gets whether a method is a batch method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>True when its first parameter is an EventBatch.</returns>
    internal static bool IsBatchMethod(IMethodSymbol method, Compilation compilation) =>
        method.MethodKind == MethodKind.Ordinary && method.Parameters.Length > 0 &&
        Is(method.Parameters[0].Type, WellKnownTypes.EventBatchName, compilation);

    /// <summary>
    /// Gets the event types a method declares with [Observes&lt;T&gt;].
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Each observed type with the attribute declaring it.</returns>
    internal static IEnumerable<(ITypeSymbol EventType, AttributeData Attribute)> Observed(IMethodSymbol method, Compilation compilation)
    {
        var observes = compilation.GetTypeByMetadataName(WellKnownTypes.ObservesAttributeName);
        if (observes is null)
        {
            yield break;
        }

        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass is { IsGenericType: true } attributeClass &&
                SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, observes))
            {
                yield return (attributeClass.TypeArguments[0], attribute);
            }
        }
    }

    /// <summary>
    /// Gets the kind of observer a type is.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <param name="evidence">The <see cref="ExplicitRegistrationEvidence"/> of the compilation.</param>
    /// <returns>The <see cref="BatchObserverKind"/>.</returns>
    internal static BatchObserverKind KindOf(INamedTypeSymbol type, Compilation compilation, ExplicitRegistrationEvidence evidence)
    {
        if (WellKnownTypes.ImplementsIReducer(type, compilation) || evidence.IsReducer(type) || WellKnownTypes.HasAttribute(type, WellKnownTypes.ReducerAttributeName))
        {
            return BatchObserverKind.Reducer;
        }

        if (WellKnownTypes.ImplementsIReactor(type, compilation) || evidence.IsReactor(type) || WellKnownTypes.HasAttribute(type, WellKnownTypes.ReactorAttributeName))
        {
            return BatchObserverKind.Reactor;
        }

        if (SelfReducerPolicy.CanBeSelf(type, compilation) &&
            (SelfReducerPolicy.IsSelf(type, compilation) || SelfReducerPolicy.Methods(type).Any(method => IsSelfBatchShaped(method, type, compilation))))
        {
            return BatchObserverKind.SelfReducer;
        }

        return BatchObserverKind.None;
    }

    /// <summary>
    /// Gets the read model of a reducer.
    /// </summary>
    /// <param name="type">The reducer type.</param>
    /// <param name="kind">The <see cref="BatchObserverKind"/> of the type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>The read model, or null when it cannot be told.</returns>
    internal static ITypeSymbol? ReadModelOf(INamedTypeSymbol type, BatchObserverKind kind, Compilation compilation)
    {
        if (kind == BatchObserverKind.SelfReducer)
        {
            return type;
        }

        var reducerFor = compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName);
        return type.AllInterfaces
            .FirstOrDefault(contract => SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, reducerFor))?
            .TypeArguments.FirstOrDefault();
    }

    /// <summary>
    /// Gets whether a method of an observer is a per-event handler method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="kind">The <see cref="BatchObserverKind"/> of its type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <param name="evidence">The <see cref="ExplicitRegistrationEvidence"/> of the compilation.</param>
    /// <returns>True when the client dispatches single events to it.</returns>
    internal static bool IsPerEventMethod(IMethodSymbol method, BatchObserverKind kind, Compilation compilation, ExplicitRegistrationEvidence evidence)
    {
        if (method.MethodKind != MethodKind.Ordinary || method.Parameters.Length == 0 || IsBatchMethod(method, compilation) ||
            !evidence.IsEventType(method.Parameters[0].Type, compilation))
        {
            return false;
        }

        return kind switch
        {
            BatchObserverKind.Reactor => true,
            BatchObserverKind.Reducer => method is { IsStatic: false, DeclaredAccessibility: Accessibility.Public },
            BatchObserverKind.SelfReducer => method.IsStatic && string.Equals(method.Name, "Reduce", StringComparison.Ordinal),
            _ => false
        };
    }

    /// <summary>
    /// Gets the methods of a type and its base types.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The ordinary methods.</returns>
    internal static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type) => SelfReducerPolicy.Methods(type);

    /// <summary>
    /// Gets whether two event types can describe the same event, because one is the other or converts to it.
    /// </summary>
    /// <param name="first">The first type.</param>
    /// <param name="second">The second type.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>True when an event of one can be an event of the other.</returns>
    internal static bool Overlaps(ITypeSymbol first, ITypeSymbol second, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(first, second) ||
        compilation.ClassifyCommonConversion(first, second) is { IsImplicit: true, IsUserDefined: false, IsReference: true } ||
        compilation.ClassifyCommonConversion(second, first) is { IsImplicit: true, IsUserDefined: false, IsReference: true };

    /// <summary>
    /// Gets where an attribute is written, falling back to a symbol's location.
    /// </summary>
    /// <param name="attribute">The <see cref="AttributeData"/>.</param>
    /// <param name="fallback">The symbol to fall back to.</param>
    /// <returns>The <see cref="Location"/>.</returns>
    internal static Location? LocationOf(AttributeData attribute, ISymbol fallback) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback.Locations.FirstOrDefault();

    /// <summary>
    /// Gets whether a type is the named type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="metadataName">The metadata name.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>True when it is.</returns>
    internal static bool Is(ITypeSymbol type, string metadataName, Compilation compilation)
    {
        var named = compilation.GetTypeByMetadataName(metadataName);
        return named is not null && SymbolEqualityComparer.Default.Equals(type, named);
    }

    static bool IsSelfBatchShaped(IMethodSymbol method, INamedTypeSymbol type, Compilation compilation) =>
        method.IsStatic && IsBatchMethod(method, compilation) && method.Parameters.Length > 1 &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, type);
}

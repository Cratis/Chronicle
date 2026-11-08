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
    /// The name a self-reducing read model's methods must have for the client to discover them.
    /// </summary>
    internal const string ReduceMethodName = "Reduce";

    /// <summary>
    /// Gets whether a method is a batch method, for analyzers that have not resolved the <see cref="BatchSymbols"/>.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>True when its first parameter is an EventBatch.</returns>
    internal static bool IsBatchMethod(IMethodSymbol method, Compilation compilation) =>
        method.MethodKind == MethodKind.Ordinary && method.Parameters.Length > 0 &&
        method.Parameters[0].Type.Name == "EventBatch" &&
        BatchSymbols.Is(method.Parameters[0].Type, compilation.GetTypeByMetadataName(WellKnownTypes.EventBatchName));

    /// <summary>
    /// Gets whether a method is a batch method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>True when its first parameter is an EventBatch.</returns>
    internal static bool IsBatchMethod(IMethodSymbol method, BatchSymbols symbols) =>
        method.MethodKind == MethodKind.Ordinary && method.Parameters.Length > 0 &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, symbols.EventBatch);

    /// <summary>
    /// Gets the event types a method declares with [Observes&lt;T&gt;].
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>Each observed type with the attribute declaring it.</returns>
    internal static IEnumerable<(ITypeSymbol EventType, AttributeData Attribute)> Observed(IMethodSymbol method, BatchSymbols symbols)
    {
        if (symbols.Observes is null)
        {
            yield break;
        }

        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass is { IsGenericType: true } attributeClass &&
                SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, symbols.Observes))
            {
                yield return (attributeClass.TypeArguments[0], attribute);
            }
        }
    }

    /// <summary>
    /// Gets the kind of observer a type is.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>The <see cref="BatchObserverKind"/>.</returns>
    internal static BatchObserverKind KindOf(INamedTypeSymbol type, BatchSymbols symbols)
    {
        var compilation = symbols.Compilation;
        if (WellKnownTypes.ImplementsIReducer(type, compilation) || symbols.Evidence.IsReducer(type) || WellKnownTypes.HasAttribute(type, WellKnownTypes.ReducerAttributeName))
        {
            return BatchObserverKind.Reducer;
        }

        if (WellKnownTypes.ImplementsIReactor(type, compilation) || symbols.Evidence.IsReactor(type) || WellKnownTypes.HasAttribute(type, WellKnownTypes.ReactorAttributeName))
        {
            return BatchObserverKind.Reactor;
        }

        if (SelfReducerPolicy.CanBeSelf(type, compilation) &&
            (SelfReducerPolicy.IsSelf(type, compilation) || SelfReducerPolicy.Methods(type).Any(method => IsSelfBatchShaped(method, type, symbols))))
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
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>The read model, or null when it cannot be told.</returns>
    internal static ITypeSymbol? ReadModelOf(INamedTypeSymbol type, BatchObserverKind kind, BatchSymbols symbols)
    {
        if (kind == BatchObserverKind.SelfReducer)
        {
            return type;
        }

        return type.AllInterfaces
            .FirstOrDefault(contract => BatchSymbols.Is(contract.OriginalDefinition, symbols.ReducerFor))?
            .TypeArguments.FirstOrDefault();
    }

    /// <summary>
    /// Gets whether a method of an observer is a per-event handler method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="observer">The observer type the method belongs to, directly or through a base type.</param>
    /// <param name="kind">The <see cref="BatchObserverKind"/> of the observer.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>True when the client dispatches single events to it.</returns>
    /// <remarks>
    /// For a reducer this is the shape the client's reducer invoker looks for, at any accessibility: a reduce method
    /// takes the event and the current read model, optionally followed by the EventContext, and returns the read model.
    /// </remarks>
    internal static bool IsPerEventMethod(IMethodSymbol method, INamedTypeSymbol observer, BatchObserverKind kind, BatchSymbols symbols)
    {
        if (method.MethodKind != MethodKind.Ordinary || method.Parameters.Length == 0 || IsBatchMethod(method, symbols))
        {
            return false;
        }

        return kind switch
        {
            BatchObserverKind.Reactor => symbols.Evidence.IsEventType(method.Parameters[0].Type, symbols.Compilation),
            BatchObserverKind.Reducer => IsReduceShaped(method, observer, symbols, symbols.EventContext),
            BatchObserverKind.SelfReducer => method.IsStatic && string.Equals(method.Name, ReduceMethodName, StringComparison.Ordinal) &&
                symbols.Evidence.IsEventType(method.Parameters[0].Type, symbols.Compilation),
            _ => false
        };
    }

    /// <summary>
    /// Gets whether a method of a reducer has the reduce shape, but takes a BatchContext where a reduce method takes the EventContext.
    /// Callers refuse it only in a reducer without a batch method; see <see cref="HasBatchMethod"/>.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="observer">The reducer type.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>True when it does.</returns>
    internal static bool IsReduceShapedWithBatchContext(IMethodSymbol method, INamedTypeSymbol observer, BatchSymbols symbols) =>
        symbols.BatchContext is not null && method.Parameters.Length == 3 && IsReduceShaped(method, observer, symbols, symbols.BatchContext);

    /// <summary>
    /// Gets whether a type or one of its base types declares a batch method.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>True when it does.</returns>
    /// <remarks>
    /// In a reducer with a batch method, a reduce-shaped method taking the BatchContext is a helper the batch method calls,
    /// not a reducer method that was meant to receive single events.
    /// </remarks>
    internal static bool HasBatchMethod(INamedTypeSymbol type, BatchSymbols symbols) =>
        MethodsOf(type).Any(method => IsBatchMethod(method, symbols));

    /// <summary>
    /// Gets the methods of a type and its base types.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The ordinary methods.</returns>
    internal static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type) => SelfReducerPolicy.Methods(type);

    /// <summary>
    /// Gets whether two event types can describe the same event.
    /// </summary>
    /// <param name="first">The first type.</param>
    /// <param name="second">The second type.</param>
    /// <param name="symbols">The <see cref="BatchSymbols"/> of the compilation.</param>
    /// <returns>True when an event of one can be an event of the other.</returns>
    /// <remarks>
    /// The client expands each observed type to the known event types assignable to it and compares those, so two
    /// interfaces overlap when one known event type implements both, even though neither converts to the other.
    /// </remarks>
    internal static bool Overlaps(ITypeSymbol first, ITypeSymbol second, BatchSymbols symbols)
    {
        var compilation = symbols.Compilation;
        if (SymbolEqualityComparer.Default.Equals(first, second) || ConvertsTo(first, second, compilation) || ConvertsTo(second, first, compilation))
        {
            return true;
        }

        if (!CanBeShared(first) || !CanBeShared(second))
        {
            return false;
        }

        return symbols.KnownEventTypes.Any(eventType => ConvertsTo(eventType, first, compilation) && ConvertsTo(eventType, second, compilation));
    }

    /// <summary>
    /// Gets where an attribute is written, falling back to a symbol's location.
    /// </summary>
    /// <param name="attribute">The <see cref="AttributeData"/>.</param>
    /// <param name="fallback">The symbol to fall back to.</param>
    /// <returns>The <see cref="Location"/>.</returns>
    internal static Location? LocationOf(AttributeData attribute, ISymbol fallback) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback.Locations.FirstOrDefault();

    static bool IsReduceShaped(IMethodSymbol method, INamedTypeSymbol observer, BatchSymbols symbols, INamedTypeSymbol? third)
    {
        if (method.IsStatic || method.Parameters.Length is not (2 or 3) ||
            (method.DeclaredAccessibility == Accessibility.Private && !SymbolEqualityComparer.Default.Equals(method.ContainingType, observer)))
        {
            return false;
        }

        var readModel = ReadModelOf(observer, BatchObserverKind.Reducer, symbols);
        if (readModel is null || !SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, readModel) ||
            (method.Parameters.Length == 3 && !BatchSymbols.Is(method.Parameters[2].Type, third)) ||
            !ReturnsReduceResult(method.ReturnType, readModel, symbols))
        {
            return false;
        }

        return symbols.IsEventType(method.Parameters[0].Type);
    }

    static bool ReturnsReduceResult(ITypeSymbol returnType, ITypeSymbol readModel, BatchSymbols symbols) =>
        SymbolEqualityComparer.Default.Equals(returnType, readModel) ||
        BatchSymbols.Is(returnType, symbols.Task) ||
        symbols.IsTaskOf(returnType, readModel) ||
        IsNullableOf(returnType, readModel) ||
        (returnType is INamedTypeSymbol { IsGenericType: true } task && BatchSymbols.Is(task.OriginalDefinition, symbols.TaskOfT) && IsNullableOf(task.TypeArguments[0], readModel));

    static bool IsNullableOf(ITypeSymbol type, ITypeSymbol readModel) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable &&
        SymbolEqualityComparer.Default.Equals(nullable.TypeArguments[0], readModel);

    static bool ConvertsTo(ITypeSymbol from, ITypeSymbol to, Compilation compilation) =>
        compilation.ClassifyCommonConversion(from, to) is { IsImplicit: true, IsUserDefined: false, IsReference: true };

    static bool CanBeShared(ITypeSymbol type) =>
        type is { SpecialType: not SpecialType.System_Object, IsSealed: false, TypeKind: TypeKind.Class or TypeKind.Interface };

    static bool IsSelfBatchShaped(IMethodSymbol method, INamedTypeSymbol type, BatchSymbols symbols) =>
        method.IsStatic && IsBatchMethod(method, symbols) && method.Parameters.Length > 1 &&
        SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, type);
}

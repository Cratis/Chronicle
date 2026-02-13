// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Helper methods for working with Chronicle types.
/// </summary>
public static class WellKnownTypes
{
    /// <summary>
    /// The full name of the EventType attribute.
    /// </summary>
    public const string EventTypeAttributeName = "Cratis.Chronicle.Concepts.Events.EventTypeAttribute";

    /// <summary>
    /// The full name of IEventSequence interface.
    /// </summary>
    public const string IEventSequenceName = "Cratis.Chronicle.EventSequences.IEventSequence";

    /// <summary>
    /// The full name of IReactor interface.
    /// </summary>
    public const string IReactorName = "Cratis.Chronicle.Reactors.IReactor";

    /// <summary>
    /// The full name of IReducer interface.
    /// </summary>
    public const string IReducerName = "Cratis.Chronicle.Reducers.IReducer";

    /// <summary>
    /// The full name of EventContext class.
    /// </summary>
    public const string EventContextName = "Cratis.Chronicle.Events.EventContext";

    /// <summary>
    /// Check if a type has the EventType attribute.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns>True if the type has the EventType attribute, false otherwise.</returns>
    public static bool HasEventTypeAttribute(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == EventTypeAttributeName);
    }

    /// <summary>
    /// Check if a type implements IReactor.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True if the type implements IReactor, false otherwise.</returns>
    public static bool ImplementsIReactor(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var reactorInterface = compilation.GetTypeByMetadataName(IReactorName);
        return reactorInterface != null && typeSymbol.AllInterfaces.Contains(reactorInterface, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Check if a type implements IReducer.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True if the type implements IReducer, false otherwise.</returns>
    public static bool ImplementsIReducer(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var reducerInterface = compilation.GetTypeByMetadataName(IReducerName);
        return reducerInterface != null && typeSymbol.AllInterfaces.Contains(reducerInterface, SymbolEqualityComparer.Default);
    }
}

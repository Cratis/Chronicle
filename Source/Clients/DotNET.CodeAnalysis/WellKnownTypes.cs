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
    /// The full name of the kernel EventType attribute.
    /// </summary>
    public const string KernelEventTypeAttributeName = "Cratis.Chronicle.Concepts.Events.EventTypeAttribute";

    /// <summary>
    /// The full name of the client EventType attribute.
    /// </summary>
    public const string ClientEventTypeAttributeName = "Cratis.Chronicle.Events.EventTypeAttribute";

    /// <summary>
    /// The full name of the EventStore attribute.
    /// </summary>
    public const string EventStoreAttributeName = "Cratis.Chronicle.Events.EventStoreAttribute";

    /// <summary>
    /// The full name of the EventSequence attribute.
    /// </summary>
    public const string EventSequenceAttributeName = "Cratis.Chronicle.EventSequences.EventSequenceAttribute";

    /// <summary>
    /// The full name of the EventLog attribute.
    /// </summary>
    public const string EventLogAttributeName = "Cratis.Chronicle.EventSequences.EventLogAttribute";

    /// <summary>
    /// The full name of the Reactor attribute.
    /// </summary>
    public const string ReactorAttributeName = "Cratis.Chronicle.Reactors.ReactorAttribute";

    /// <summary>
    /// The full name of the Reducer attribute.
    /// </summary>
    public const string ReducerAttributeName = "Cratis.Chronicle.Reducers.ReducerAttribute";

    /// <summary>
    /// The sentinel value representing the default event store.
    /// </summary>
    public const string DefaultEventStoreName = "";

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
        {
            var attributeName = attr.AttributeClass?.ToDisplayString();
            return attributeName == KernelEventTypeAttributeName ||
                   attributeName == ClientEventTypeAttributeName;
        });
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

    /// <summary>
    /// Get the event store name from a type's <see cref="EventStoreAttributeName"/> attribute, or from its containing assembly.
    /// </summary>
    /// <remarks>
    /// First checks for a type-level <see cref="EventStoreAttributeName"/> attribute. If not found,
    /// falls back to an assembly-level <see cref="EventStoreAttributeName"/> attribute on the type's containing assembly.
    /// </remarks>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns>The event store name, or <see langword="null"/> if neither the type nor its containing assembly has the attribute.</returns>
    public static string? GetEventStoreName(ITypeSymbol typeSymbol)
    {
        var typeAttribute = typeSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == EventStoreAttributeName);

        if (typeAttribute is not null)
        {
            return typeAttribute.ConstructorArguments.Length > 0
                ? typeAttribute.ConstructorArguments[0].Value as string
                : null;
        }

        var assemblyAttribute = typeSymbol.ContainingAssembly?.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == EventStoreAttributeName);

        if (assemblyAttribute is not null)
        {
            return assemblyAttribute.ConstructorArguments.Length > 0
                ? assemblyAttribute.ConstructorArguments[0].Value as string
                : null;
        }

        return null;
    }

    /// <summary>
    /// Gets the explicit event store name for a type, or the default event store sentinel when none is defined.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the event store for.</param>
    /// <returns>The explicit event store name, or <see cref="DefaultEventStoreName"/> when no attribute is defined.</returns>
    public static string GetEventStoreNameOrDefault(ITypeSymbol typeSymbol) =>
        GetEventStoreName(typeSymbol) ?? DefaultEventStoreName;

    /// <summary>
    /// Formats an event store name for diagnostics.
    /// </summary>
    /// <param name="eventStoreName">The event store name to format.</param>
    /// <returns>A diagnostic-friendly event store display value.</returns>
    public static string FormatEventStoreName(string eventStoreName) =>
        eventStoreName.Length == 0 ? "<default>" : eventStoreName;
}

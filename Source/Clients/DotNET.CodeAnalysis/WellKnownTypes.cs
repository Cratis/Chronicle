// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    /// The full name of the OnceOnly attribute.
    /// </summary>
    public const string OnceOnlyAttributeName = "Cratis.Chronicle.Reactors.OnceOnlyAttribute";

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
    /// The full name of the EventForEventSourceId type.
    /// </summary>
    public const string EventForEventSourceIdName = "Cratis.Chronicle.EventSequences.EventForEventSourceId";

    /// <summary>
    /// The full name of IEventLog interface.
    /// </summary>
    public const string IEventLogName = "Cratis.Chronicle.EventSequences.IEventLog";

    /// <summary>
    /// The full name of ICommandPipeline interface.
    /// </summary>
    public const string ICommandPipelineName = "Cratis.Chronicle.Commands.ICommandPipeline";

    /// <summary>
    /// The full name of IProjectionFor generic interface (open generic).
    /// </summary>
    public const string IProjectionForName = "Cratis.Chronicle.Projections.IProjectionFor`1";

    /// <summary>
    /// The full name of IConstraint interface.
    /// </summary>
    public const string IConstraintName = "Cratis.Chronicle.Events.Constraints.IConstraint";

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

    /// <summary>
    /// Check if a type implements IProjectionFor&lt;T&gt;.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True if the type implements IProjectionFor&lt;T&gt;, false otherwise.</returns>
    public static bool ImplementsIProjectionFor(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var projectionForInterface = compilation.GetTypeByMetadataName(IProjectionForName);
        if (projectionForInterface is null)
        {
            return false;
        }

        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, projectionForInterface));
    }

    /// <summary>
    /// Check if a type implements IConstraint.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True if the type implements IConstraint, false otherwise.</returns>
    public static bool ImplementsIConstraint(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var constraintInterface = compilation.GetTypeByMetadataName(IConstraintName);
        if (constraintInterface is null)
        {
            return false;
        }

        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, constraintInterface));
    }

    /// <summary>
    /// Check if a constructor parameter type is IEventLog or ICommandPipeline (by full name).
    /// </summary>
    /// <param name="parameterType">The parameter type symbol to check.</param>
    /// <returns>True if the type is IEventLog or ICommandPipeline, false otherwise.</returns>
    public static bool IsEventLogOrCommandPipeline(ITypeSymbol parameterType)
    {
        var fullName = parameterType.ToDisplayString();
        return fullName == IEventLogName || fullName == ICommandPipelineName;
    }

    /// <summary>
    /// Check if a constructor parameter type is IEventLog (by full name).
    /// </summary>
    /// <param name="parameterType">The parameter type symbol to check.</param>
    /// <returns>True if the type is IEventLog, false otherwise.</returns>
    public static bool IsIEventLog(ITypeSymbol parameterType) =>
        parameterType.ToDisplayString() == IEventLogName;

    /// <summary>
    /// Check whether a type is <see cref="System.Linq.Expressions.Expression{TDelegate}"/>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>True if the type is Expression&lt;TDelegate&gt;, false otherwise.</returns>
    public static bool IsExpressionType(ITypeSymbol? type) =>
        type is INamedTypeSymbol { IsGenericType: true } named &&
        named.OriginalDefinition.ToDisplayString() == "System.Linq.Expressions.Expression<TDelegate>";

    /// <summary>
    /// Determines whether an expression is a pure member-access chain (identifiers and member accesses only).
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression is a pure member-access chain, false otherwise.</returns>
    public static bool IsPureMemberAccessChain(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax => true,
            MemberAccessExpressionSyntax memberAccess => IsPureMemberAccessChain(memberAccess.Expression),
            _ => false
        };

    /// <summary>
    /// Determines whether a statement is considered imperative (not a pure builder call).
    /// </summary>
    /// <param name="statement">The statement to check.</param>
    /// <returns>True if the statement is imperative, false otherwise.</returns>
    public static bool IsImperativeStatement(StatementSyntax statement) =>
        statement is IfStatementSyntax or
        ForStatementSyntax or
        ForEachStatementSyntax or
        WhileStatementSyntax or
        DoStatementSyntax or
        SwitchStatementSyntax or
        ReturnStatementSyntax or
        ThrowStatementSyntax or
        LocalDeclarationStatementSyntax or
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax };
}

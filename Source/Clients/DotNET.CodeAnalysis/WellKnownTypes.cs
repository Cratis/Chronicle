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
    /// The full name of the Arc model-bound Command attribute.
    /// </summary>
    /// <remarks>
    /// Matched by full-name string so the analyzer does not need a reference to the Arc assembly.
    /// </remarks>
    public const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";

    /// <summary>
    /// The full name of the OnceOnly attribute.
    /// </summary>
    public const string OnceOnlyAttributeName = "Cratis.Chronicle.Reactors.OnceOnlyAttribute";

    /// <summary>
    /// The full name of the ReplayAttribute type.
    /// </summary>
    public const string ReplayAttributeName = "Cratis.Chronicle.Reactors.ReplayAttribute";

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
    /// The full name of the IAggregateRoot interface.
    /// </summary>
    /// <remarks>
    /// Matched by full-name string so the analyzer does not need a reference to the Arc assembly.
    /// </remarks>
    public const string IAggregateRootName = "Cratis.Arc.Chronicle.Aggregates.IAggregateRoot";

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
    /// The full name of the Key attribute.
    /// </summary>
    public const string KeyAttributeName = "Cratis.Chronicle.Keys.KeyAttribute";

    /// <summary>
    /// The full name of the Subject attribute.
    /// </summary>
    public const string SubjectAttributeName = "Cratis.Chronicle.SubjectAttribute";

    /// <summary>
    /// The full name of the NoAutoMap attribute.
    /// </summary>
    public const string NoAutoMapAttributeName = "Cratis.Chronicle.Projections.NoAutoMapAttribute";

    /// <summary>
    /// The full name of the PII attribute.
    /// </summary>
    public const string PiiAttributeName = "Cratis.Chronicle.Compliance.GDPR.PIIAttribute";

    /// <summary>
    /// The full name of the Arc model-bound ReadModel attribute.
    /// </summary>
    /// <remarks>
    /// Matched by full-name string so the analyzer does not need a reference to the Arc assembly.
    /// </remarks>
    /// <summary>
    /// The full name of the open generic SetFromContext attribute.
    /// </summary>
    public const string SetFromContextAttributeName = "Cratis.Chronicle.Projections.ModelBound.SetFromContextAttribute<TEvent>";

    /// <summary>
    /// The full name of the ReadModel attribute.
    /// </summary>
    public const string ReadModelAttributeName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <summary>
    /// The open-generic display string of the EventTypeMigration&lt;TUpgrade, TPrevious&gt; base class.
    /// </summary>
    public const string EventTypeMigrationGenericDisplay = "Cratis.Chronicle.Events.Migrations.EventTypeMigration<TUpgrade, TPrevious>";

    /// <summary>
    /// The full name of the EventStreamId attribute.
    /// </summary>
    public const string EventStreamIdAttributeName = "Cratis.Chronicle.Events.EventStreamIdAttribute";

    /// <summary>
    /// The full name of the ICanProvideEventStreamId interface.
    /// </summary>
    public const string ICanProvideEventStreamIdName = "Cratis.Chronicle.Events.ICanProvideEventStreamId";

    /// <summary>
    /// The full name of the open generic IMongoCollection interface.
    /// </summary>
    public const string IMongoCollectionName = "MongoDB.Driver.IMongoCollection`1";

    /// <summary>
    /// The open-generic display string of the client EventSourceId&lt;T&gt; type.
    /// </summary>
    public const string ClientEventSourceIdGenericDisplay = "Cratis.Chronicle.Events.EventSourceId<T>";

    /// <summary>
    /// The open-generic display string of the kernel EventSourceId&lt;T&gt; type.
    /// </summary>
    public const string KernelEventSourceIdGenericDisplay = "Cratis.Chronicle.Concepts.Events.EventSourceId<T>";

    /// <summary>
    /// Check whether a type is, or derives from, the strongly-typed <c>EventSourceId&lt;T&gt;</c>.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>True if the type is or derives from <c>EventSourceId&lt;T&gt;</c>, false otherwise.</returns>
    public static bool DerivesFromEventSourceId(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol { IsGenericType: true } named)
            {
                var definition = named.OriginalDefinition.ToDisplayString();
                if (definition == ClientEventSourceIdGenericDisplay ||
                    definition == KernelEventSourceIdGenericDisplay)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check whether a symbol carries an attribute with the given full name.
    /// </summary>
    /// <param name="symbol">The symbol to inspect.</param>
    /// <param name="attributeFullName">The full display name of the attribute type.</param>
    /// <returns>True if the symbol carries the attribute, false otherwise.</returns>
    public static bool HasAttribute(ISymbol symbol, string attributeFullName) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeFullName);

    /// <summary>
    /// Find the <c>EventTypeMigration&lt;TUpgrade, TPrevious&gt;</c> base type of a type, if any.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns>The constructed <c>EventTypeMigration&lt;TUpgrade, TPrevious&gt;</c> base, or <see langword="null"/> when the type does not derive from it.</returns>
    public static INamedTypeSymbol? GetEventTypeMigrationBase(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol { IsGenericType: true } named &&
                named.OriginalDefinition.ToDisplayString() == EventTypeMigrationGenericDisplay)
            {
                return named;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the <c>[EventType]</c> attribute data from a type, whether the client or kernel attribute.
    /// </summary>
    /// <param name="type">The type symbol to inspect.</param>
    /// <returns>The <see cref="AttributeData"/> for the <c>[EventType]</c> attribute, or <see langword="null"/> when the type has none.</returns>
    public static AttributeData? GetEventTypeAttributeData(ITypeSymbol type) =>
        type.GetAttributes().FirstOrDefault(attribute =>
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            return attributeName == KernelEventTypeAttributeName ||
                   attributeName == ClientEventTypeAttributeName;
        });

    /// <summary>
    /// Get the explicit id argument from an <c>[EventType]</c> attribute, or <see langword="null"/> when no explicit id was supplied.
    /// </summary>
    /// <param name="attributeData">The <c>[EventType]</c> attribute data.</param>
    /// <returns>The explicit id string, or <see langword="null"/> when the id is absent or empty.</returns>
    public static string? GetEventTypeExplicitId(AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var id = attributeData.ConstructorArguments[0].Value as string;
        return string.IsNullOrEmpty(id) ? null : id;
    }

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
    /// Check if a type implements IAggregateRoot.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>True if the type implements IAggregateRoot, false otherwise.</returns>
    public static bool ImplementsIAggregateRoot(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var aggregateRootInterface = compilation.GetTypeByMetadataName(IAggregateRootName);
        return aggregateRootInterface is not null && typeSymbol.AllInterfaces.Contains(aggregateRootInterface, SymbolEqualityComparer.Default);
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
    /// Gets the event store a type is explicitly pinned to, if it is pinned to one at all.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the event store for.</param>
    /// <returns>The explicit event store name, or <see langword="null"/> when the type names no event store.</returns>
    /// <remarks>
    /// Absence of the attribute is not a store. It means unconstrained - whatever the host is configured with -
    /// so an unattributed type is compatible with any single named store and can never be what makes an artifact
    /// span two of them. Standing a sentinel in for it and counting that alongside real names reported the
    /// ordinary shape where a host declares its own event types locally and imports a few from a contracts
    /// assembly that pins the store name for cross-host routing.
    /// </remarks>
    public static string? GetExplicitEventStoreName(ITypeSymbol typeSymbol) => GetEventStoreName(typeSymbol);

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
    /// Determines whether an expression is a supported projection property accessor - a member-access chain rooted in a lambda parameter.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> used to resolve the root identifier.</param>
    /// <returns>True if the expression is a member-access chain that bottoms out at a lambda parameter, false otherwise.</returns>
    /// <remarks>
    /// This is stricter than <see cref="IsPureMemberAccessChain(ExpressionSyntax)"/>: it rejects member-access chains rooted in
    /// something other than the lambda parameter (for example <c>_ =&gt; DateTimeOffset.UtcNow</c>, which reads a static member and
    /// ignores the parameter), as well as a bare parameter reference that maps no property. Projection builder accessors extract a
    /// property path at definition time and are never executed, so only <c>parameter.Property</c> chains are valid.
    /// </remarks>
    public static bool IsProjectionPropertyAccessor(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not MemberAccessExpressionSyntax)
        {
            return false;
        }

        var current = expression;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            current = memberAccess.Expression;
        }

        return current is IdentifierNameSyntax identifier &&
               semanticModel.GetSymbolInfo(identifier).Symbol is IParameterSymbol;
    }

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

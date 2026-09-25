// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if types appended to event sequences have the EventType attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventSequenceAppendAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EventTypeMustHaveAttributeWhenAppended,
        title: "Event type must have [EventType] attribute",
        messageFormat: "Type '{0}' must be marked with [EventType] attribute when appended to an event sequence",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [EventType] attribute registers a type in Chronicle's event type registry, which is required for serialization, routing, and replay. Add [EventType(\"<guid>\")] to the class being appended, or replace it with a dedicated event type class already marked with [EventType].");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check if this is a call to Append or AppendMany on IEventSequence
        if (!IsEventSequenceAppendMethod(methodSymbol))
        {
            return;
        }

        // Resolve against the declared parameter, not the call-site position: the wrapped-event
        // overload has events first, and named arguments can appear in any order.
        if (context.SemanticModel.GetOperation(invocation) is not IInvocationOperation operation)
        {
            return;
        }

        var eventOperation = operation.Arguments.FirstOrDefault(argument =>
            argument.Parameter?.Name == (methodSymbol.Name == "Append" ? "event" : "events"));
        if (eventOperation?.Syntax is not ArgumentSyntax eventArgument ||
            (methodSymbol.Name == "AppendMany" && eventOperation.Parameter?.Type is INamedTypeSymbol { IsGenericType: true } parameterType &&
             parameterType.TypeArguments[0].ToDisplayString() == WellKnownTypes.EventForEventSourceIdName))
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(eventArgument.Expression);
        if (typeInfo.Type == null)
        {
            return;
        }

        // For AppendMany, we need to check the element type of the collection
        var eventType = GetEventType(typeInfo.Type, methodSymbol);
        if (eventType == null || eventType.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        // Check if the event type has the EventType attribute
        if (!WellKnownTypes.HasEventTypeAttribute(eventType))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                eventArgument.GetLocation(),
                eventType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    static bool IsEventSequenceAppendMethod(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Name != "Append" && methodSymbol.Name != "AppendMany")
        {
            return false;
        }

        var containingType = methodSymbol.ContainingType;
        while (containingType != null)
        {
            if (containingType.ToDisplayString() == WellKnownTypes.IEventSequenceName)
            {
                return true;
            }

            foreach (var @interface in containingType.AllInterfaces)
            {
                if (@interface.ToDisplayString() == WellKnownTypes.IEventSequenceName)
                {
                    return true;
                }
            }

            containingType = containingType.BaseType;
        }

        return false;
    }

    static ITypeSymbol? GetEventType(ITypeSymbol type, IMethodSymbol methodSymbol)
    {
        // For AppendMany, extract the element type from the collection.
        if (methodSymbol.Name == "AppendMany")
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType;
            }

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var typeArguments = namedType.TypeArguments;
                if (typeArguments.Length > 0)
                {
                    return typeArguments[0];
                }
            }

            // If it's IEnumerable but not generic, we can't determine the type
            return null;
        }

        // For Append, the type is the event type itself
        return type;
    }
}

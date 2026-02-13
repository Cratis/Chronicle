// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        description: "Types appended to event sequences must be marked with the [EventType] attribute.");

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

        // Find the event argument(s)
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
        {
            return;
        }

        // For Append, check the second argument (first is eventSourceId)
        // For AppendMany, check the second argument which is IEnumerable<object>
        var eventArgument = GetEventArgument(methodSymbol, arguments);
        if (eventArgument == null)
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

    static ArgumentSyntax? GetEventArgument(IMethodSymbol methodSymbol, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // For both Append and AppendMany, the event(s) parameter is the second one (after eventSourceId)
        if (arguments.Count < 2)
        {
            return null;
        }

        return arguments[1];
    }

    static ITypeSymbol? GetEventType(ITypeSymbol type, IMethodSymbol methodSymbol)
    {
        // For AppendMany, extract the element type from IEnumerable<object>
        if (methodSymbol.Name == "AppendMany")
        {
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

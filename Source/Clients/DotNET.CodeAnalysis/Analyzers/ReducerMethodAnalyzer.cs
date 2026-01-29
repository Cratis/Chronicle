// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks reducer method signatures and event type attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReducerMethodAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor SignatureRule = new(
        id: DiagnosticIds.ReducerMethodSignatureMustMatchAllowed,
        title: "Reducer method signature must match allowed signatures",
        messageFormat: "Reducer method '{0}' must have signature: Task MethodName(TEvent event) or Task MethodName(TEvent event, EventContext context) or void MethodName(TEvent event) or void MethodName(TEvent event, EventContext context)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reducer methods must follow allowed signatures.");

    static readonly DiagnosticDescriptor EventTypeRule = new(
        id: DiagnosticIds.ReducerEventParameterMustHaveAttribute,
        title: "Reducer event parameter must have [EventType] attribute",
        messageFormat: "Event parameter type '{0}' in reducer method '{1}' must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Event parameters in reducer methods must be marked with the [EventType] attribute.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SignatureRule, EventTypeRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        // Check if the containing type implements IReducer
        if (!WellKnownTypes.ImplementsIReducer(methodSymbol.ContainingType, context.Compilation))
        {
            return;
        }

        // Skip special methods (constructors, property accessors, etc.)
        if (methodSymbol.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        // Check if this looks like an event handler method
        var parameters = methodSymbol.Parameters;
        if (parameters.Length == 0 || parameters.Length > 2)
        {
            return;
        }

        var firstParam = parameters[0];
        var firstParamType = firstParam.Type;

        // Skip if first parameter is object (can't determine event type)
        if (firstParamType.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        // Check if this could be an event handler method based on return type
        var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var validReturnType = methodSymbol.ReturnsVoid ||
            (taskType != null && SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, taskType));

        if (!validReturnType)
        {
            return;
        }

        // If it has two parameters, second must be EventContext
        if (parameters.Length == 2)
        {
            var eventContextType = context.Compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName);
            if (eventContextType == null || !SymbolEqualityComparer.Default.Equals(parameters[1].Type, eventContextType))
            {
                var diagnostic = Diagnostic.Create(
                    SignatureRule,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }

        // Check if the first parameter type has EventType attribute
        if (!WellKnownTypes.HasEventTypeAttribute(firstParamType))
        {
            var diagnostic = Diagnostic.Create(
                EventTypeRule,
                firstParam.Locations.FirstOrDefault(),
                firstParamType.Name,
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}

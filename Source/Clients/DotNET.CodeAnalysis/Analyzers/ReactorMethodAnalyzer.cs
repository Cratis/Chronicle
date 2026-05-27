// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks reactor method signatures and event type attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorMethodAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor SignatureRule = new(
        id: DiagnosticIds.ReactorMethodSignatureMustMatchAllowed,
        title: "Reactor method signature must match allowed signatures",
        messageFormat: "Reactor method '{0}' must have signature: Task MethodName(TEvent event) or Task MethodName(TEvent event, EventContext context) or Task<TResult> MethodName(TEvent event) or Task<TResult> MethodName(TEvent event, EventContext context) or void MethodName(TEvent event) or void MethodName(TEvent event, EventContext context) or TResult MethodName(TEvent event) or TResult MethodName(TEvent event, EventContext context)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle dispatches events to reactor methods by matching event types. Methods with unsupported signatures are silently skipped. Change the method to one of the allowed forms: 'void MethodName(TEvent event)', 'void MethodName(TEvent event, EventContext context)', 'Task MethodName(TEvent event)', 'Task MethodName(TEvent event, EventContext context)', 'Task<TResult> MethodName(TEvent event)', or 'Task<TResult> MethodName(TEvent event, EventContext context)', or the synchronous side-effect forms 'TResult MethodName(TEvent event)' / 'TResult MethodName(TEvent event, EventContext context)'. TResult can be an event type, ReactorSideEffect, or IEnumerable of those.");

    static readonly DiagnosticDescriptor EventTypeRule = new(
        id: DiagnosticIds.ReactorEventParameterMustHaveAttribute,
        title: "Reactor event parameter must have [EventType] attribute",
        messageFormat: "Event parameter type '{0}' in reactor method '{1}' must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle uses the [EventType] attribute to route incoming events to the correct reactor method. Without it, the event cannot be matched and the handler will never be called. Add [EventType(\"<guid>\")] to the class used as the event parameter in this method.");

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

        // Check if the containing type implements IReactor
        if (!WellKnownTypes.ImplementsIReactor(methodSymbol.ContainingType, context.Compilation))
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

        // Check if this could be an event handler method based on return type.
        // Valid return types: void, Task, Task<T>, or any reference type (sync side-effect returns such as
        // event types, ReactorSideEffect, IEnumerable<ReactorSideEffect>, etc.).
        // Value types (int, bool, struct, etc.) are not valid reactor return types.
        var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var isGenericTask = taskOfTType != null &&
            methodSymbol.ReturnType is INamedTypeSymbol namedReturn &&
            namedReturn.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedReturn.OriginalDefinition, taskOfTType);
        var validReturnType = methodSymbol.ReturnsVoid ||
            (taskType != null && SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, taskType)) ||
            isGenericTask ||
            (!methodSymbol.ReturnType.IsValueType && methodSymbol.ReturnType.SpecialType == SpecialType.None);

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

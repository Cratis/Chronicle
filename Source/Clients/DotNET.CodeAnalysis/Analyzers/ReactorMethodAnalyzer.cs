// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
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
        title: "Reactor method has a parameter that cannot be resolved",
        messageFormat: "Reactor method '{0}' has a parameter that cannot be resolved. After the event, a reactor method may take the EventContext, a read model, or a service - not a primitive, value type, or string.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A reactor handler method takes the event it reacts to as its first parameter. Any further parameters are resolved as dependencies: the EventContext, a read model (materialized from its reducer or projection), or a service from the service provider. A parameter that is a primitive, value type, or string is almost certainly a mistake and would fail to resolve at runtime.");

    static readonly DiagnosticDescriptor ReturnTypeRule = new(
        id: DiagnosticIds.ReactorMethodReturnTypeMustBeSupported,
        title: "Reactor method has an unsupported return type",
        messageFormat: "Reactor method '{0}' has unsupported return type '{1}'. Return void, Task, an event type, EventForEventSourceId, EventsWithConcurrencyScopes, a [ReactorSideEffect]-marked type, an array or IEnumerable<T> sequence of supported elements, or a Task-wrapped side effect.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle discovers reactor methods returning void, Task, a registered event type, EventForEventSourceId, EventsWithConcurrencyScopes, or a one-dimensional array or IEnumerable<T> implementation of events, targeted events, or objects. Mark custom synchronous side-effect types with [ReactorSideEffect] so the analyzer accepts them and their sequences. The marker only informs the analyzer; a registered side-effect handler must still claim and process the declared return type.");

    static readonly DiagnosticDescriptor EventTypeRule = new(
        id: DiagnosticIds.ReactorEventParameterMustHaveAttribute,
        title: "Reactor event parameter must have [EventType] attribute",
        messageFormat: "Event parameter type '{0}' in reactor method '{1}' must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle uses the [EventType] attribute to route incoming events to the correct reactor method. Without it, the event cannot be matched and the handler will never be called. Add [EventType(\"<guid>\")] to the class used as the event parameter in this method.");

    static readonly DiagnosticDescriptor AsyncVoidRule = new(
        id: DiagnosticIds.ReactorMethodMustNotBeAsyncVoid,
        title: "Reactor handler must not be async void",
        messageFormat: "Reactor handler '{0}' is async void; return Task so Chronicle can await completion and observe failures",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An async void reactor handler returns before its asynchronous work completes and prevents Chronicle from observing completion and exceptions.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SignatureRule, ReturnTypeRule, EventTypeRule, AsyncVoidRule);

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

        // Skip special methods (constructors, property accessors, etc.)
        if (methodSymbol.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        // A handler method takes the event as its first parameter; any further parameters are dependencies.
        var parameters = methodSymbol.Parameters;
        if (parameters.Length == 0)
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

        var evidence = ExplicitRegistrationEvidence.For(context.Compilation, context.CancellationToken);

        // Check if the containing type implements IReactor
        if (!WellKnownTypes.ImplementsIReactor(methodSymbol.ContainingType, context.Compilation) && !evidence.IsReactor(methodSymbol.ContainingType))
        {
            return;
        }

        AnalyzeHandler(context, methodSymbol, evidence);
    }

    static void AnalyzeHandler(SymbolAnalysisContext context, IMethodSymbol methodSymbol, ExplicitRegistrationEvidence evidence)
    {
        // An event first parameter, not a Task return type, distinguishes handlers from ordinary helpers.
        var isHandler = evidence.IsEventType(methodSymbol.Parameters[0].Type, context.Compilation);
        if (!ValidateReturnType(context, methodSymbol, evidence, isHandler))
        {
            return;
        }

        if (!ValidateEventParameter(context, methodSymbol, isHandler))
        {
            return;
        }

        if (!ValidateDependencies(context, methodSymbol))
        {
            return;
        }

        if (IsPublicInstanceAsyncVoid(methodSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(AsyncVoidRule, methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name));
        }
    }

    static bool IsPublicInstanceAsyncVoid(IMethodSymbol method) =>
        method.DeclaredAccessibility == Accessibility.Public && !method.IsStatic && method.IsAsync && method.ReturnsVoid;

    static bool ValidateReturnType(SymbolAnalysisContext context, IMethodSymbol methodSymbol, ExplicitRegistrationEvidence evidence, bool isHandler)
    {
        if (!IsSupportedReturnType(methodSymbol, context.Compilation, evidence))
        {
            if (isHandler)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ReturnTypeRule,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name,
                    methodSymbol.ReturnType.ToDisplayString()));
            }

            return false;
        }

        return true;
    }

    static bool ValidateEventParameter(SymbolAnalysisContext context, IMethodSymbol methodSymbol, bool isHandler)
    {
        if (!isHandler)
        {
            // A handler-only marker says this was intended as a handler, rather than an ordinary helper.
            if (HasHandlerOnlyMarker(methodSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    EventTypeRule,
                    methodSymbol.Parameters[0].Locations.FirstOrDefault(),
                    methodSymbol.Parameters[0].Type.Name,
                    methodSymbol.Name));
            }

            return false;
        }

        return true;
    }

    static bool ValidateDependencies(SymbolAnalysisContext context, IMethodSymbol methodSymbol)
    {
        // Reference types may come from DI or be read models; primitives and strings cannot be resolved.
        var parameters = methodSymbol.Parameters;
        var eventContextType = context.Compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName);
        for (var index = 1; index < parameters.Length; index++)
        {
            var parameterType = parameters[index].Type;
            if (eventContextType != null && SymbolEqualityComparer.Default.Equals(parameterType, eventContextType))
            {
                continue;
            }

            if (parameterType.IsValueType || parameterType.SpecialType == SpecialType.System_String)
            {
                var diagnostic = Diagnostic.Create(
                    SignatureRule,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether the method carries a marker that only makes sense on a handler.
    /// </summary>
    /// <param name="method">The <see cref="IMethodSymbol"/> to check.</param>
    /// <returns>True when it does, false otherwise.</returns>
    /// <remarks>
    /// The first parameter alone cannot separate "meant to be a handler, forgot the attribute" from "an ordinary
    /// helper" - both simply have a first parameter that is not an event type, and a reactor is full of the
    /// latter. A marker whose only meaning is on a handler is the difference: an author who wrote one has said
    /// what they intended, and Chronicle will nonetheless never dispatch to the method.
    /// </remarks>
    static bool HasHandlerOnlyMarker(IMethodSymbol method) =>
        method.GetAttributes().Any(attribute =>
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            return string.Equals(name, WellKnownTypes.OnceOnlyAttributeName, StringComparison.Ordinal) ||
                   string.Equals(name, WellKnownTypes.ReplayAttributeName, StringComparison.Ordinal);
        });

    static bool IsSupportedReturnType(IMethodSymbol method, Compilation compilation, ExplicitRegistrationEvidence evidence)
    {
        if (method.ReturnsVoid)
        {
            return true;
        }

        var returnType = method.ReturnType;

        // The runtime accepts any type assignable to Task, including Task<T> and derived task types.
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        for (var current = returnType as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (taskType != null && SymbolEqualityComparer.Default.Equals(current, taskType))
            {
                return true;
            }
        }

        return IsSupportedSyncSideEffectReturnType(returnType, compilation, evidence);
    }

    static bool IsSupportedSyncSideEffectReturnType(ITypeSymbol returnType, Compilation compilation, ExplicitRegistrationEvidence evidence) =>
        evidence.IsEventType(returnType, compilation) ||
        WellKnownTypes.HasReactorSideEffectAttribute(returnType) ||
        IsKnownType(returnType, WellKnownTypes.EventForEventSourceIdName, compilation) ||
        IsKnownType(returnType, WellKnownTypes.EventsWithConcurrencyScopesName, compilation) ||
        IsSupportedSideEffectSequence(returnType, compilation, evidence);

    static bool IsKnownType(ITypeSymbol type, string metadataName, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName(metadataName));

    static bool IsSupportedSideEffectSequence(ITypeSymbol returnType, Compilation compilation, ExplicitRegistrationEvidence evidence)
    {
        var eventForEventSourceId = compilation.GetTypeByMetadataName(WellKnownTypes.EventForEventSourceIdName);
        if (returnType is IArrayTypeSymbol array)
        {
            return array.IsSZArray && IsSupportedSequenceElement(array.ElementType, eventForEventSourceId, compilation, evidence);
        }

        if (returnType.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        return returnType is INamedTypeSymbol named &&
               named.AllInterfaces.Concat([named]).Any(candidate =>
                   candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
                   IsSupportedSequenceElement(candidate.TypeArguments[0], eventForEventSourceId, compilation, evidence));
    }

    static bool IsSupportedSequenceElement(ITypeSymbol element, INamedTypeSymbol? eventForEventSourceId, Compilation compilation, ExplicitRegistrationEvidence evidence) =>
        element.SpecialType == SpecialType.System_Object ||
        evidence.IsEventType(element, compilation) ||
        WellKnownTypes.HasReactorSideEffectAttribute(element) ||
        (eventForEventSourceId != null && SymbolEqualityComparer.Default.Equals(element, eventForEventSourceId));
}

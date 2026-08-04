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

        // What makes a method a handler is that its first parameter is an event type - that is what Chronicle's
        // own discovery keys on, over public and non-public instance methods alike. Deciding it on the return
        // type instead meant every private `async Task` helper on a reactor, the commonest shape there is, was
        // analyzed as a handler: its first parameter was demanded to be an event type and its ordinary value-type
        // arguments were reported as unresolvable dependencies. A helper returning a string or a domain record
        // escaped, purely because its return type fell outside the supported set.
        var isHandler = WellKnownTypes.HasEventTypeAttribute(firstParamType);

        // A method whose return type is not a supported reactor return shape is only a handler mistake when
        // its first parameter is unambiguously an event type. Chronicle silently skips such methods at
        // discovery, so flag them here.
        if (!IsSupportedReturnType(methodSymbol, context.Compilation))
        {
            if (isHandler)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SignatureRule,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name));
            }

            return;
        }

        if (!isHandler)
        {
            // The first parameter is not an event, so Chronicle will never dispatch to this method and its
            // parameters are nobody's dependencies. It is only worth reporting where the author has said
            // otherwise: a marker that has no meaning anywhere but on a handler.
            if (HasHandlerOnlyMarker(methodSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    EventTypeRule,
                    firstParam.Locations.FirstOrDefault(),
                    firstParamType.Name,
                    methodSymbol.Name));
            }

            return;
        }

        // Parameters after the event are dependencies: the EventContext, a read model, or a service.
        // A primitive, value type, or string can never be a read model and is almost never a service, so it
        // is reported as an unresolvable parameter.
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
                break;
            }
        }
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

    static bool IsSupportedReturnType(IMethodSymbol method, Compilation compilation)
    {
        if (method.ReturnsVoid)
        {
            return true;
        }

        var returnType = method.ReturnType;

        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskType != null && SymbolEqualityComparer.Default.Equals(returnType, taskType))
        {
            return true;
        }

        // Any Task<T> is accepted — the returned value is dispatched to a side-effect handler at runtime.
        var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        if (taskOfTType != null &&
            returnType is INamedTypeSymbol task &&
            task.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(task.OriginalDefinition, taskOfTType))
        {
            return true;
        }

        return IsSupportedSyncSideEffectReturnType(returnType, compilation);
    }

    static bool IsSupportedSyncSideEffectReturnType(ITypeSymbol returnType, Compilation compilation)
    {
        if (WellKnownTypes.HasEventTypeAttribute(returnType))
        {
            return true;
        }

        var eventForEventSourceId = compilation.GetTypeByMetadataName(WellKnownTypes.EventForEventSourceIdName);
        if (eventForEventSourceId != null && SymbolEqualityComparer.Default.Equals(returnType, eventForEventSourceId))
        {
            return true;
        }

        if (returnType is INamedTypeSymbol named &&
            named.IsGenericType &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            var element = named.TypeArguments[0];
            return element.SpecialType == SpecialType.System_Object ||
                   WellKnownTypes.HasEventTypeAttribute(element) ||
                   (eventForEventSourceId != null && SymbolEqualityComparer.Default.Equals(element, eventForEventSourceId));
        }

        return false;
    }
}

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
        messageFormat: "Reducer method '{0}' must return TReadModel, Task<TReadModel> or Task and take (TEvent event, TReadModel? current) with an optional EventContext third parameter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle dispatches events to reducer methods with an event first, the current read model second and an optional EventContext third. Supported return types are the read model, Task of the read model and Task. Methods with other signatures are skipped.");

    static readonly DiagnosticDescriptor EventTypeRule = new(
        id: DiagnosticIds.ReducerEventParameterMustHaveAttribute,
        title: "Reducer event parameter must have [EventType] attribute",
        messageFormat: "Event parameter type '{0}' in reducer method '{1}' must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle uses the [EventType] attribute to route incoming events to the correct reducer method. Without it, the event cannot be matched and the handler will never be called. Use a concrete event type marked with [EventType] rather than object.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SignatureRule, EventTypeRule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var eventTypes = GetTypes(start.Compilation.Assembly.GlobalNamespace).Where(WellKnownTypes.HasEventTypeAttribute).ToImmutableArray();
            start.RegisterSymbolAction(symbol => AnalyzeMethod(symbol, eventTypes), SymbolKind.Method);
        });
    }

    static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> eventTypes)
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

        var reducerFor = context.Compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName);
        var readModel = methodSymbol.ContainingType.AllInterfaces.FirstOrDefault(_ =>
            SymbolEqualityComparer.Default.Equals(_.OriginalDefinition, reducerFor))?.TypeArguments.FirstOrDefault();
        var parameters = methodSymbol.Parameters;
        if (readModel is null || parameters.Length == 0 ||
            (!parameters.Any(_ => SymbolEqualityComparer.Default.Equals(_.Type, readModel)) &&
             !WellKnownTypes.HasEventTypeAttribute(parameters[0].Type) &&
             !ReturnsReadModel(methodSymbol.ReturnType, readModel, context.Compilation)))
        {
            return;
        }

        var eventContext = context.Compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName);
        if (methodSymbol.IsStatic || parameters.Length is < 2 or > 3 || parameters.Any(_ => _.RefKind != RefKind.None) ||
            !SymbolEqualityComparer.Default.Equals(parameters[1].Type, readModel) ||
            (parameters.Length == 3 && !SymbolEqualityComparer.Default.Equals(parameters[2].Type, eventContext)) ||
            !HasSupportedReturnType(methodSymbol, readModel, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(SignatureRule, methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name));
            return;
        }

        if (!IsEventType(parameters[0].Type, eventTypes, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventTypeRule,
                parameters[0].Locations.FirstOrDefault(),
                parameters[0].Type.Name,
                methodSymbol.Name));
        }
    }

    static bool IsEventType(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> eventTypes, Compilation compilation) =>
        type.SpecialType != SpecialType.System_Object &&
        (WellKnownTypes.HasEventTypeAttribute(type) || eventTypes.Any(eventType => compilation.ClassifyCommonConversion(eventType, type) is { IsImplicit: true, IsUserDefined: false }));

    static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceOrTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers().OfType<INamespaceOrTypeSymbol>())
        {
            if (member is INamedTypeSymbol type)
            {
                yield return type;
            }

            foreach (var nested in GetTypes(member))
            {
                yield return nested;
            }
        }
    }

    static bool HasSupportedReturnType(IMethodSymbol method, ITypeSymbol readModel, Compilation compilation)
    {
        var returnType = method.ReturnType;
        return SymbolEqualityComparer.Default.Equals(returnType, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
            ReturnsReadModel(returnType, readModel, compilation);
    }

    static bool ReturnsReadModel(ITypeSymbol returnType, ITypeSymbol readModel, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(returnType, readModel) ||
        (returnType is INamedTypeSymbol { IsGenericType: true } named &&
         SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) &&
         SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], readModel));
}

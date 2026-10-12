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
            var evidence = new Lazy<ExplicitRegistrationEvidence>(() => ExplicitRegistrationEvidence.Create(start.Compilation, start.CancellationToken));
            var eventTypes = new Lazy<ImmutableArray<INamedTypeSymbol>>(() =>
                GetTypes(start.Compilation.GlobalNamespace).Where(HasEventTypeAttribute)
                    .Concat(evidence.Value.EventTypes.OfType<INamedTypeSymbol>()).Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToImmutableArray());
            start.RegisterSymbolAction(symbol => AnalyzeMethod(symbol, eventTypes, evidence.Value), SymbolKind.Method);
        });
    }

    static void AnalyzeMethod(SymbolAnalysisContext context, Lazy<ImmutableArray<INamedTypeSymbol>> eventTypes, ExplicitRegistrationEvidence evidence)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        // Check if the containing type implements IReducer
        if (!WellKnownTypes.ImplementsIReducer(methodSymbol.ContainingType, context.Compilation) && !evidence.IsReducer(methodSymbol.ContainingType))
        {
            return;
        }

        // Non-public and static methods can be ordinary helpers, not intended reducer methods.
        if (!IsPublicInstanceMethod(methodSymbol))
        {
            return;
        }

        var readModel = GetReadModel(methodSymbol.ContainingType, context.Compilation) ??
            (evidence.IsReducer(methodSymbol.ContainingType) ? InferReadModel(methodSymbol.ContainingType, eventTypes, context.Compilation) : null);
        if (readModel is null || !LooksLikeReducerMethod(methodSymbol, readModel, eventTypes, context.Compilation))
        {
            return;
        }

        if (!HasDispatchableSignature(methodSymbol, readModel, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(SignatureRule, methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name));
            return;
        }

        var parameters = methodSymbol.Parameters;
        if (!IsEventType(parameters[0].Type, eventTypes, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EventTypeRule,
                parameters[0].Locations.FirstOrDefault(),
                parameters[0].Type.Name,
                methodSymbol.Name));
        }
    }

    static ITypeSymbol? GetReadModel(INamedTypeSymbol reducerType, Compilation compilation)
    {
        var reducerFor = compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName);
        return reducerType.AllInterfaces
            .FirstOrDefault(_ => SymbolEqualityComparer.Default.Equals(_.OriginalDefinition, reducerFor))?
            .TypeArguments.FirstOrDefault();
    }

    static ITypeSymbol? InferReadModel(INamedTypeSymbol reducer, Lazy<ImmutableArray<INamedTypeSymbol>> eventTypes, Compilation compilation)
    {
        var candidates = reducer.GetMembers().OfType<IMethodSymbol>()
            .Where(method => IsPublicInstanceMethod(method) && HasInstanceParameterShape(method) &&
                IsEventType(method.Parameters[0].Type, eventTypes, compilation) &&
                HasOptionalEventContext(method.Parameters, compilation) && HasSupportedReturnType(method, method.Parameters[1].Type, compilation))
            .Select(method => method.Parameters[1].Type).Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    static bool IsPublicInstanceMethod(IMethodSymbol method) =>
        method is { MethodKind: MethodKind.Ordinary, IsStatic: false, DeclaredAccessibility: Accessibility.Public };

    static bool LooksLikeReducerMethod(IMethodSymbol method, ITypeSymbol readModel, Lazy<ImmutableArray<INamedTypeSymbol>> eventTypes, Compilation compilation)
    {
        var parameters = method.Parameters;
        if (parameters.Length == 0)
        {
            return false;
        }

        return parameters.Any(_ => SymbolEqualityComparer.Default.Equals(_.Type, readModel)) ||
            IsEventType(parameters[0].Type, eventTypes, compilation) ||
            ReturnsReadModel(method.ReturnType, readModel, compilation);
    }

    static bool HasDispatchableSignature(IMethodSymbol method, ITypeSymbol readModel, Compilation compilation)
    {
        if (!HasInstanceParameterShape(method))
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, readModel) &&
            HasOptionalEventContext(method.Parameters, compilation) &&
            HasSupportedReturnType(method, readModel, compilation);
    }

    static bool HasInstanceParameterShape(IMethodSymbol method) =>
        !method.IsStatic &&
        method.Parameters.Length is 2 or 3 &&
        method.Parameters.All(_ => _.RefKind == RefKind.None);

    static bool HasOptionalEventContext(ImmutableArray<IParameterSymbol> parameters, Compilation compilation) =>
        parameters.Length == 2 ||
        SymbolEqualityComparer.Default.Equals(parameters[2].Type, compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName));

    static bool IsEventType(ITypeSymbol type, Lazy<ImmutableArray<INamedTypeSymbol>> eventTypes, Compilation compilation) =>
        type.SpecialType != SpecialType.System_Object &&
        (HasEventTypeAttribute(type) || eventTypes.Value.Any(@event => SymbolEqualityComparer.Default.Equals(@event, type)) || IsAssignableEventType(type, eventTypes, compilation));

    static bool IsAssignableEventType(ITypeSymbol type, Lazy<ImmutableArray<INamedTypeSymbol>> eventTypes, Compilation compilation) =>
        type is { IsSealed: false, TypeKind: TypeKind.Interface or TypeKind.Class } &&
        eventTypes.Value.Any(eventType => compilation.ClassifyCommonConversion(eventType, type) is { IsImplicit: true, IsUserDefined: false });

    static bool HasEventTypeAttribute(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (HasEventTypeMarker(current))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasEventTypeMarker(ITypeSymbol type) =>
        WellKnownTypes.HasEventTypeAttribute(type) || WellKnownTypes.HasEventTypeGenerationForAttribute(type);

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

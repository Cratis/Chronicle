// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if reactor methods returning event side effects are marked with [OnceOnly].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorOnceOnlyAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorReturningEventsMustBeOnceOnly,
        title: "Reactor methods returning event side effects must be marked with [OnceOnly]",
        messageFormat: "Reactor method '{0}' returns event side effects but is not marked with [OnceOnly]. During replay operations, this will append duplicate events to the event log.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactor handler methods that return events as side effects will cause those events to be appended to the event log. During replay operations (redaction, revision, observer rewind), these events would be appended again, which is usually undesirable and can lead to duplicate events and incorrect state. Mark the method with [OnceOnly] to ensure it executes only once and is skipped during replays.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

        // Check if this method already has the OnceOnly attribute (on method or class level)
        if (HasOnceOnlyAttribute(methodSymbol) || HasOnceOnlyAttribute(methodSymbol.ContainingType))
        {
            return;
        }

        // Check if this method returns event side effects
        if (!ReturnsEventSideEffects(methodSymbol, context.Compilation))
        {
            return;
        }

        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            Rule,
            methodSymbol.Locations.FirstOrDefault(),
            methodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    static bool HasOnceOnlyAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == WellKnownTypes.OnceOnlyAttributeName);
    }

    static bool ReturnsEventSideEffects(IMethodSymbol methodSymbol, Compilation compilation)
    {
        // Get the actual return type (unwrap Task<T> if needed)
        var returnType = GetActualReturnType(methodSymbol, compilation);

        // void and Task are not side effects
        if (returnType == null)
        {
            return false;
        }

        // Check if the return type is an event type (has EventType attribute)
        if (WellKnownTypes.HasEventTypeAttribute(returnType))
        {
            return true;
        }

        // Check if the return type is IEnumerable<T> where T is an event type
        if (IsEnumerableOfEventTypes(returnType, compilation))
        {
            return true;
        }

        return false;
    }

    static ITypeSymbol? GetActualReturnType(IMethodSymbol methodSymbol, Compilation compilation)
    {
        // If method returns void, there's no return type
        if (methodSymbol.ReturnsVoid)
        {
            return null;
        }

        var returnType = methodSymbol.ReturnType;

        // Check if it's Task (non-generic)
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskType != null && SymbolEqualityComparer.Default.Equals(returnType, taskType))
        {
            return null;
        }

        // Check if it's Task<T> and unwrap to T
        var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        if (taskOfTType != null &&
            returnType is INamedTypeSymbol namedReturn &&
            namedReturn.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedReturn.OriginalDefinition, taskOfTType))
        {
            // Return the T from Task<T>
            return namedReturn.TypeArguments.FirstOrDefault();
        }

        // Otherwise, return the type as-is
        return returnType;
    }

    static bool IsEnumerableOfEventTypes(ITypeSymbol typeSymbol, Compilation compilation)
    {
        // Check if type implements IEnumerable<T>
        var enumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        if (enumerableOfT == null)
        {
            return false;
        }

        // Check if the type itself is IEnumerable<T>
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, enumerableOfT))
        {
            var typeArgument = namedType.TypeArguments.FirstOrDefault();
            return typeArgument != null && WellKnownTypes.HasEventTypeAttribute(typeArgument);
        }

        // Find IEnumerable<T> in the type's interfaces
        var enumerableInterface = typeSymbol.AllInterfaces
            .FirstOrDefault(i =>
                i.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, enumerableOfT));

        if (enumerableInterface == null)
        {
            return false;
        }

        // Check if T (the element type) has EventType attribute
        var elementType = enumerableInterface.TypeArguments.FirstOrDefault();
        return elementType != null && WellKnownTypes.HasEventTypeAttribute(elementType);
    }
}

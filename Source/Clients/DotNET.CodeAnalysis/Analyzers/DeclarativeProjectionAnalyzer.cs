// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks if declarative projection methods reference event types with the EventType attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeclarativeProjectionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The name every projection-builder method gives the type parameter that takes the event.
    /// </summary>
    /// <remarks>
    /// The whole builder API follows it - From, Join, RemovedWith, RemovedWithJoin, ClearWith and
    /// FromEventProperty all name it TEvent, and every other type parameter names what it actually is
    /// (TChildModel, TNestedModel, TProperty, TKeyType). Keying on the declared parameter rather than an
    /// allow-list of method names means a builder method added later is covered without being enumerated here.
    /// </remarks>
    const string EventTypeParameterName = "TEvent";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DeclarativeProjectionEventTypeMustHaveAttribute,
        title: "Declarative projection event type must have [EventType] attribute",
        messageFormat: "Type '{0}' used in declarative projection must be marked with [EventType] attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle uses the [EventType] attribute to identify and route events to the correct projection handler during replay. Add [EventType(\"<guid>\")] to the type referenced in this projection, or replace it with a type that is already marked with [EventType].");

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

        // Check if this is a method call on a projection builder
        if (invocation.Expression is not MemberAccessExpressionSyntax)
        {
            return;
        }

        if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
        {
            return;
        }

        // Check if the method name indicates a projection builder method (From, Join, etc.)
        if (!IsProjectionBuilderMethod(methodSymbol))
        {
            return;
        }

        // Check if the method has generic type arguments
        if (methodSymbol is not IMethodSymbol { IsGenericMethod: true } genericMethod)
        {
            return;
        }

        // Only the argument in an event position can be an event. A builder method's other type parameters are
        // the child read-model type of Children<TChildModel>, the key type of IdentifiedBy<TProperty>, the
        // join-key property type of On<TProperty> and so on - none of which is, or could be, an event type.
        // Checking every argument reported correct code at every one of those call sites, and because they are
        // inferred rather than written the diagnostic pointed at a call with no visible type argument at all.
        var definition = genericMethod.OriginalDefinition;
        for (var index = 0; index < definition.TypeParameters.Length && index < genericMethod.TypeArguments.Length; index++)
        {
            if (definition.TypeParameters[index].Name != EventTypeParameterName)
            {
                continue;
            }

            var typeArgument = genericMethod.TypeArguments[index];
            if (typeArgument.SpecialType == SpecialType.System_Object)
            {
                continue;
            }

            if (!WellKnownTypes.HasEventTypeAttribute(typeArgument))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    typeArgument.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    static bool IsProjectionBuilderMethod(IMethodSymbol methodSymbol)
    {
        // Check if the containing type is a projection builder
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return false;
        }

        // Use the original generic definition to avoid false matches where an unrelated
        // type appears only in a type argument (e.g. IReadModelPropertiesBuilder<T, E, IFromBuilder<T, E>>
        // would contain "IFromBuilder" in its bound display string but is not itself a builder type
        // that accepts event type arguments).
        var typeName = containingType.OriginalDefinition.ToDisplayString();

        // Check for IProjectionBuilderFor<T> or related interfaces
        return typeName.Contains("IProjectionBuilderFor") ||
               typeName.Contains("IProjectionBuilder") ||
               typeName.Contains("IFromBuilder") ||
               typeName.Contains("IJoinBuilder") ||
               typeName.Contains("IChildrenBuilder");
    }
}

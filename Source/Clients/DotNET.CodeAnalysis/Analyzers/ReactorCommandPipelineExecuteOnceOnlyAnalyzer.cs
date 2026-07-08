// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a reactor handler invoking <c>ICommandPipeline.Execute</c> is marked with <c>[OnceOnly]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorCommandPipelineExecuteOnceOnlyAnalyzer : DiagnosticAnalyzer
{
    const string ExecuteMethodName = "Execute";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorCommandPipelineExecuteMustBeOnceOnly,
        title: "Reactor handlers invoking ICommandPipeline.Execute must be marked with [OnceOnly]",
        messageFormat: "Reactor handler '{0}' invokes ICommandPipeline.Execute but is not marked [OnceOnly]; replay will re-execute the command. Mark the method [OnceOnly].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A reactor handler that calls ICommandPipeline.Execute produces a side effect. During replay operations (redaction, revision, observer rewind), the handler runs again and re-executes the command, duplicating the side effect. Mark the method with [OnceOnly] so it is skipped during replays.");

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

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.Name != ExecuteMethodName || !IsCommandPipeline(methodSymbol.ContainingType, context.Compilation))
        {
            return;
        }

        var methodDeclaration = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol enclosingMethod)
        {
            return;
        }

        if (!WellKnownTypes.ImplementsIReactor(enclosingMethod.ContainingType, context.Compilation))
        {
            return;
        }

        if (HasOnceOnlyAttribute(enclosingMethod) || HasOnceOnlyAttribute(enclosingMethod.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            enclosingMethod.Name));
    }

    static bool IsCommandPipeline(ITypeSymbol? typeSymbol, Compilation compilation)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        var commandPipelineInterface = compilation.GetTypeByMetadataName(WellKnownTypes.ICommandPipelineName);
        if (commandPipelineInterface is null)
        {
            return typeSymbol.ToDisplayString() == WellKnownTypes.ICommandPipelineName;
        }

        if (SymbolEqualityComparer.Default.Equals(typeSymbol, commandPipelineInterface))
        {
            return true;
        }

        return typeSymbol.AllInterfaces.Contains(commandPipelineInterface, SymbolEqualityComparer.Default);
    }

    static bool HasOnceOnlyAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == WellKnownTypes.OnceOnlyAttributeName);
}

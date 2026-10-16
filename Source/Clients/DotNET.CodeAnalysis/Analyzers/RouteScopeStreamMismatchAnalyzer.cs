// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reports statically visible definition-linked scope stream mismatches on single-route appends.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RouteScopeStreamMismatchAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RouteScopeStreamMismatch,
        "Scope stream does not match append",
        "Scope for '{0}.{1}' does not match append to '{0}.{2}'; use a labelled independent condition to intentionally guard another stream",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        description: "A scope built with ForEventSource<T> must match a single-route append through the same definition. Labelled APIs permit independent guards.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    static void Analyze(OperationAnalysisContext context)
    {
        var append = (IInvocationOperation)context.Operation;
        if (!IsRoutedAppend(append.TargetMethod)) return;
        var definition = Definition(append);
        var stream = Argument(append, "eventStream")?.ConstantValue;
        var scopeArgument = append.Arguments.FirstOrDefault(_ => _.Parameter?.Name == "concurrencyScope");
        if (definition is null || stream is not { HasValue: true, Value: string appendStream } || scopeArgument is null) return;
        var scope = FindDefinitionScope(scopeArgument.Value, append.SemanticModel!, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
        if (scope is null || !SymbolEqualityComparer.Default.Equals(definition, Definition(scope))) return;
        var scopeStream = Argument(scope, "stream")?.ConstantValue;
        if (scopeStream is not { HasValue: true, Value: string guardedStream } || guardedStream == appendStream) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, scopeArgument.Value.Syntax.GetLocation(), definition.Name, guardedStream, appendStream));
    }

    static bool IsRoutedAppend(IMethodSymbol method)
    {
        var owner = method.ContainingType.ToDisplayString();
        return owner switch
        {
            "Cratis.Chronicle.EventSequences.IEventSequence" or "Cratis.Chronicle.EventSequences.EventSequence" =>
                method.Name == "AppendThroughEventSource" || method.Name == "AppendManyThroughEventSource" || method.Name == "AppendPreparedThroughEventSource",
            "Cratis.Chronicle.EventSequences.EventSequenceEventSourceExtensions" => method.Name == "Append" || method.Name == "AppendMany" || method.Name == "AppendPrepared",
            _ => false
        };
    }

    static IOperation? Argument(IInvocationOperation invocation, string name) => invocation.Arguments.FirstOrDefault(_ => _.Parameter?.Name == name)?.Value;

    static ITypeSymbol? Definition(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.TypeArguments.Length == 1) return invocation.TargetMethod.TypeArguments[0];
        return Unwrap(Argument(invocation, "eventSource")) is ITypeOfOperation typeOf ? typeOf.TypeOperand : null;
    }

    static IOperation? Unwrap(IOperation? operation) => operation is IConversionOperation conversion ? Unwrap(conversion.Operand) : operation;

    static IInvocationOperation? FindDefinitionScope(IOperation operation, SemanticModel model, HashSet<ISymbol> visited)
    {
        operation = Unwrap(operation)!;
        if (operation is ILocalReferenceOperation local)
        {
            if (local.Local.Type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString() != "Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope" || !visited.Add(local.Local)) return null;
            var declaration = local.Local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            if (declaration?.Initializer is null || IsReassigned(local.Local, declaration, model)) return null;
            var initializer = model.GetOperation(declaration.Initializer.Value);
            return initializer is null ? null : FindDefinitionScope(initializer, model, visited);
        }
        if (operation is not IInvocationOperation invocation || invocation.TargetMethod.ContainingType.ToDisplayString() != "Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScopeBuilder") return null;
        if (invocation.TargetMethod.Name == "ForEventSource") return invocation;

        // A later stream override erases the statically visible ForEventSource stream.
        if (invocation.TargetMethod.Name == "WithEventStreamType") return null;
        return invocation.Instance is null ? null : FindDefinitionScope(invocation.Instance, model, visited);
    }

    static bool IsReassigned(ILocalSymbol local, VariableDeclaratorSyntax declaration, SemanticModel model)
    {
        var body = declaration.FirstAncestorOrSelf<BlockSyntax>();
        if (body is null) return true;
        return body.DescendantNodes().OfType<IdentifierNameSyntax>().Any(_ =>
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(_).Symbol, local) &&
            ((_.Parent is AssignmentExpressionSyntax assignment && assignment.Left == _) ||
             (_.Parent is ArgumentSyntax argument && argument.RefKindKeyword.RawKind != 0)));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reports invalid event stream declarations on read models and event source definitions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventStreamReadModelAnalyzer : DiagnosticAnalyzer
{
    const string DeclarationName = "Cratis.Chronicle.ReadModels.EventStreamAttribute<TSource>";
    const string StreamName = "Cratis.Chronicle.EventSources.EventStreamAttribute";
    const string SourceName = "Cratis.Chronicle.EventSources.IEventSource";
    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.InvalidEventStreamReadModel,
        "Invalid read model event stream",
        "'{0}' has an invalid event stream declaration: {1}",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterOperationAction(AnalyzeKey, OperationKind.Invocation);
    }

    static AttributeData? Declaration(INamedTypeSymbol type) => type.GetAttributes().FirstOrDefault(attribute =>
        attribute.AttributeClass?.OriginalDefinition.ToDisplayString() == DeclarationName);

    static bool IsSource(INamedTypeSymbol type) => type.AllInterfaces.Any(contract => contract.ToDisplayString() == SourceName);

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var attribute in type.GetAttributes().Where(attribute => attribute.AttributeClass?.ToDisplayString() == StreamName && !IsSource(type)))
        {
            Report(context, type, attribute, "Use [EventStream<TSource>] on a read model; [EventStream] declares a stream on an IEventSource.");
        }
        if (Declaration(type) is not { } declaration) return;
        if (IsSource(type))
        {
            Report(context, type, declaration, "Use the non-generic [EventStream] on an IEventSource.");
            return;
        }
        var stream = declaration.ConstructorArguments.FirstOrDefault().Value as string;
        var source = declaration.AttributeClass!.TypeArguments[0] as INamedTypeSymbol;
        if (string.IsNullOrWhiteSpace(stream) || stream == "All" || source?.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == StreamName && attribute.ConstructorArguments.FirstOrDefault().Value as string == stream) != true)
        {
            Report(context, type, declaration, $"'{stream}' must name one stream declared by the event source definition.");
        }
        if (SelfReducerPolicy.IsSelf(type, context.Compilation) || WellKnownTypes.ImplementsIReducer(type, context.Compilation) ||
            SelfReducerPolicy.Types(context.Compilation.Assembly.GlobalNamespace).Any(builder => builder.AllInterfaces.Any(contract =>
                SelfReducerPolicy.Equal(contract.OriginalDefinition, context.Compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName)) &&
                SelfReducerPolicy.Equal(contract.TypeArguments.FirstOrDefault(), type))))
        {
            Report(context, type, declaration, "Reducers do not support stream-scoped folds; use a source-keyed projection.");
        }
    }

    static void AnalyzeKey(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (method.Name != "UsingKeyFromContext" ||
            method.ContainingType.OriginalDefinition.ToDisplayString() != "Cratis.Chronicle.Projections.KeyBuilder<TReadModel, TEvent, TBuilder>" ||
            method.ContainingType.TypeArguments[0] is not INamedTypeSymbol model || Declaration(model) is null)
        {
            return;
        }
        if (!invocation.Arguments.Any(argument => argument.Value.DescendantsAndSelf().OfType<IPropertyReferenceOperation>().Any(property =>
            property.Property.Name == "EventStreamId" && property.Property.ContainingType.ToDisplayString() == "Cratis.Chronicle.Events.EventContext")))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), model.Name, "A per-stream read model must be keyed by event source id, not stream id."));
    }

    static void Report(SymbolAnalysisContext context, INamedTypeSymbol type, AttributeData attribute, string reason) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations.FirstOrDefault(), type.Name, reason));
}

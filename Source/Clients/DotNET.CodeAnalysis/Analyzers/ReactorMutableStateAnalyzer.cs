// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a class implementing IReactor does not declare mutable instance state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorMutableStateAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorMustNotHaveMutableState,
        title: "Reactor must not have mutable state",
        messageFormat: "Reactor '{0}' declares mutable state '{1}'; reactors must be stateless. Use readonly, primary-constructor-injected dependencies.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactors observe events and produce side effects; they are re-created and replayed by Chronicle, so any mutable instance state is unreliable and leaks context between invocations. Keep reactors stateless: express dependencies as readonly, primary-constructor-injected fields, and derive everything else from the event and event context.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (!WellKnownTypes.ImplementsIReactor(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (!IsMutableState(member))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                namedTypeSymbol.Name,
                member.Name));
        }
    }

    static bool IsMutableState(ISymbol member) => member switch
    {
        IFieldSymbol field => IsMutableField(field),
        IPropertySymbol property => IsMutableProperty(property),
        _ => false
    };

    static bool IsMutableField(IFieldSymbol field) =>
        !field.IsStatic &&
        !field.IsConst &&
        !field.IsReadOnly &&
        !field.IsImplicitlyDeclared;

    static bool IsMutableProperty(IPropertySymbol property) =>
        !property.IsStatic &&
        property.SetMethod is { IsInitOnly: false };
}

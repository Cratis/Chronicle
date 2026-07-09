// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a class implementing IReactor does not inject a storage primitive such as IMongoCollection&lt;T&gt;.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorStorageAccessAnalyzer : DiagnosticAnalyzer
{
    const string MongoCollectionOpenGenericDisplay = "MongoDB.Driver.IMongoCollection<T>";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReactorMustNotAccessStorageDirectly,
        title: "Reactor must not access storage directly",
        messageFormat: "Reactor '{0}' injects storage primitive '{1}' directly. Read keyed state through an injected read model parameter or IReadModels.GetInstanceById instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactors should express intent, not reach into the persistence layer. Injecting a storage primitive such as IMongoCollection<T> couples the reactor to a sink and bypasses Chronicle's read-model abstraction. Read keyed state through an injected read model parameter or IReadModels.GetInstanceById instead.");

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

        foreach (var constructor in namedTypeSymbol.Constructors)
        {
            if (constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            var diagnostics = constructor.Parameters
                .Where(parameter => IsMongoCollection(parameter.Type, context.Compilation))
                .Select(parameter => Diagnostic.Create(
                    Rule,
                    parameter.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name,
                    parameter.Name));

            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    static bool IsMongoCollection(ITypeSymbol parameterType, Compilation compilation)
    {
        if (parameterType is not INamedTypeSymbol named)
        {
            return false;
        }

        var mongoCollection = compilation.GetTypeByMetadataName(WellKnownTypes.IMongoCollectionName);
        if (mongoCollection is not null &&
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, mongoCollection))
        {
            return true;
        }

        return named.OriginalDefinition.ToDisplayString() == MongoCollectionOpenGenericDisplay;
    }
}

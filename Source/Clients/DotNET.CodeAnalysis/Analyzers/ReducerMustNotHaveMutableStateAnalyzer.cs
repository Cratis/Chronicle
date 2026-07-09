// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that checks that a class implementing IReducer does not declare mutable instance state
/// or inject a storage primitive such as IMongoCollection&lt;T&gt; directly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReducerMustNotHaveMutableStateAnalyzer : DiagnosticAnalyzer
{
    const string MongoCollectionOpenGenericDisplay = "MongoDB.Driver.IMongoCollection<T>";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReducerMustNotHaveMutableState,
        title: "Reducer must not have mutable state",
        messageFormat: "Reducer '{0}' declares mutable state '{1}'. Reducers must be stateless for deterministic replay.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reducers build read-model state by folding events; Chronicle re-creates and replays them, so any mutable instance state or direct storage access makes the result depend on which events happened to run through a particular instance. Keep reducers stateless and deterministic: express dependencies as readonly, primary-constructor-injected fields, read keyed state through an injected read model or IReadModels.GetInstanceById instead of a storage primitive, and derive everything else from the current state and the event.");

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

        if (!WellKnownTypes.ImplementsIReducer(namedTypeSymbol, context.Compilation))
        {
            return;
        }

        foreach (var member in namedTypeSymbol.GetMembers().Where(IsMutableState))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                namedTypeSymbol.Name,
                member.Name));
        }

        foreach (var constructor in namedTypeSymbol.Constructors.Where(constructor => !constructor.IsImplicitlyDeclared))
        {
            foreach (var parameter in constructor.Parameters.Where(parameter => IsStoragePrimitive(parameter.Type, context.Compilation)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    parameter.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name,
                    parameter.Name));
            }
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

    static bool IsStoragePrimitive(ITypeSymbol parameterType, Compilation compilation)
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

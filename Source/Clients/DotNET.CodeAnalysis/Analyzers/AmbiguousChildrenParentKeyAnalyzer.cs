// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when a model-bound <c>[ChildrenFrom]</c> collection omits <c>parentKey</c> and the
/// parent key cannot be inferred unambiguously because the event has more than one property of the parent
/// read model's identifier type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AmbiguousChildrenParentKeyAnalyzer : DiagnosticAnalyzer
{
    const string ChildrenFromAttributeName = "ChildrenFromAttribute";
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string KeyParameterName = "key";
    const string ParentKeyParameterName = "parentKey";
    const string IdentifierName = "Id";

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AmbiguousChildrenParentKey,
        title: "Ambiguous parent key for [ChildrenFrom] collection",
        messageFormat: "Cannot unambiguously infer the parent key for the [ChildrenFrom] collection '{0}': event '{1}' has more than one property of the parent identifier type '{2}'. Specify the parent key explicitly via the 'parentKey' argument.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a [ChildrenFrom] collection omits parentKey, Chronicle infers it by matching the parent read model's identifier type against the event's properties (excluding the child key). When more than one property still matches, the inference is ambiguous and resolves by declaration order, which is fragile. Specify the parentKey argument to make the relationship explicit.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        var identifierType = GetIdentifierType(typeSymbol);
        if (identifierType is null)
        {
            return;
        }

        foreach (var (member, attribute) in GetChildrenFromAttributes(typeSymbol))
        {
            AnalyzeChildrenFrom(context, member, attribute, identifierType);
        }
    }

    static void AnalyzeChildrenFrom(SymbolAnalysisContext context, ISymbol member, AttributeData attribute, ITypeSymbol identifierType)
    {
        if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass ||
            attributeClass.TypeArguments[0] is not INamedTypeSymbol eventType)
        {
            return;
        }

        var (childKey, parentKey, resolved) = ReadKeyArguments(attribute);

        // Only inference is at risk — an explicit parentKey (or unreadable arguments) is left alone.
        if (!resolved || parentKey is not null)
        {
            return;
        }

        var matches = eventType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property =>
                !property.IsStatic &&
                SymbolEqualityComparer.Default.Equals(property.Type, identifierType) &&
                !string.Equals(property.Name, childKey, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length < 2)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            member.Locations.FirstOrDefault(),
            member.Name,
            eventType.Name,
            identifierType.Name));
    }

    static ITypeSymbol? GetIdentifierType(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(property => !property.IsStatic && string.Equals(property.Name, IdentifierName, System.StringComparison.OrdinalIgnoreCase))
            ?.Type;

    static IEnumerable<(ISymbol Member, AttributeData Attribute)> GetChildrenFromAttributes(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attribute in property.GetAttributes().Where(IsChildrenFrom))
            {
                yield return (property, attribute);
            }
        }

        // For a positional record, [ChildrenFrom] without an explicit target lands on the constructor
        // parameter rather than the generated property, so the parameters must be inspected too.
        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.Parameters)
            {
                foreach (var attribute in parameter.GetAttributes().Where(IsChildrenFrom))
                {
                    yield return (parameter, attribute);
                }
            }
        }
    }

    static bool IsChildrenFrom(AttributeData attribute) =>
        attribute.AttributeClass is { } attributeClass &&
        string.Equals(attributeClass.Name, ChildrenFromAttributeName, System.StringComparison.Ordinal) &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;

    static (string? ChildKey, string? ParentKey, bool Resolved) ReadKeyArguments(AttributeData attribute)
    {
        var constructor = attribute.AttributeConstructor;
        if (constructor is null)
        {
            return (null, null, false);
        }

        string? childKey = null;
        string? parentKey = null;
        var arguments = attribute.ConstructorArguments;

        for (var i = 0; i < constructor.Parameters.Length && i < arguments.Length; i++)
        {
            var value = arguments[i].Value as string;
            switch (constructor.Parameters[i].Name)
            {
                case KeyParameterName:
                    childKey = value;
                    break;

                case ParentKeyParameterName:
                    parentKey = value;
                    break;
            }
        }

        return (childKey, parentKey, true);
    }
}

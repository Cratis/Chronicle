// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that warns when a model-bound <c>[ChildrenFrom]</c> child carries a collection property that has no
/// same-named property on the child-creating event and no explicit mapping — so AutoMap fills it from nothing
/// and it silently projects as an empty collection.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ChildrenCollectionPropertyAutoMapAnalyzer : DiagnosticAnalyzer
{
    const string ChildrenFromAttributeName = "ChildrenFromAttribute";
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string KeysNamespace = "Cratis.Chronicle.Keys";
    const string KeyAttributeName = "KeyAttribute";
    const string IdentifiedByParameterName = "identifiedBy";

    static readonly HashSet<string> _mappingAttributeNames =
    [
        "SetFromAttribute",
        "SetFromContextAttribute",
        "SetValueAttribute",
        "AddFromAttribute",
        "SubtractFromAttribute",
        "CountAttribute",
        "IncrementAttribute",
        "DecrementAttribute",
        "JoinAttribute",
        "ChildrenFromAttribute",
        "NestedAttribute",
        "FromEveryAttribute",
        "RemovedWithAttribute",
        "RemovedWithJoinAttribute",
        "ClearWithAttribute"
    ];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ChildrenCollectionPropertyAutoMapsToNothing,
        title: "[ChildrenFrom] child collection property auto-maps to nothing",
        messageFormat: "The [ChildrenFrom] child collection property '{0}' has no property named '{0}' on event '{1}' and no explicit mapping, so it will always project as an empty collection. Rename it to match an event property, or map it explicitly with [SetFrom<{1}>(nameof(...))].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A collection property on a [ChildrenFrom] child is filled by AutoMap, which matches the event's property of the same name. When no source event carries a matching property and nothing maps it explicitly, the property silently projects as an empty collection. Rename the property to match the event, or bridge the names with [SetFrom<TEvent>(nameof(...))].");

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

        foreach (var (member, attribute) in GetChildrenFromAttributes(typeSymbol))
        {
            AnalyzeChildrenFrom(context, member, attribute);
        }
    }

    static void AnalyzeChildrenFrom(SymbolAnalysisContext context, ISymbol member, AttributeData attribute)
    {
        if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass ||
            attributeClass.TypeArguments[0] is not INamedTypeSymbol eventType)
        {
            return;
        }

        var childType = GetChildElementType(GetMemberType(member));
        if (childType is null)
        {
            return;
        }

        var identifiedBy = ReadIdentifiedBy(attribute);
        var eventPropertyNames = eventType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsStatic)
            .Select(property => property.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, type, attributes) in GetChildMembers(childType))
        {
            if (!IsCollection(type) ||
                IsChildKey(name, attributes, identifiedBy) ||
                HasExplicitMapping(attributes) ||
                eventPropertyNames.Contains(name))
            {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? member.Locations.FirstOrDefault();
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, name, eventType.Name));
        }
    }

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

    static IEnumerable<(string Name, ITypeSymbol Type, ImmutableArray<AttributeData> Attributes)> GetChildMembers(INamedTypeSymbol childType)
    {
        // Prefer the primary constructor of a positional record — that is where the client reads per-property
        // mapping attributes ([Key], [SetFrom], ...). Fall back to properties for non-record child types.
        var primaryConstructor = childType.InstanceConstructors
            .Where(constructor => constructor.Parameters.Length > 0)
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is not null)
        {
            var propertiesByName = childType.GetMembers()
                .OfType<IPropertySymbol>()
                .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in primaryConstructor.Parameters)
            {
                var attributes = parameter.GetAttributes();
                if (propertiesByName.TryGetValue(parameter.Name, out var property))
                {
                    attributes = attributes.AddRange(property.GetAttributes());
                }

                yield return (parameter.Name, parameter.Type, attributes);
            }

            yield break;
        }

        foreach (var property in childType.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            yield return (property.Name, property.Type, property.GetAttributes());
        }
    }

    static bool IsChildKey(string name, ImmutableArray<AttributeData> attributes, string? identifiedBy)
    {
        if (identifiedBy is not null && string.Equals(name, identifiedBy, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return attributes.Any(attribute =>
            attribute.AttributeClass is { } attributeClass &&
            string.Equals(attributeClass.Name, KeyAttributeName, StringComparison.Ordinal) &&
            attributeClass.ContainingNamespace?.ToDisplayString() == KeysNamespace);
    }

    static bool HasExplicitMapping(ImmutableArray<AttributeData> attributes) =>
        attributes.Any(attribute =>
            attribute.AttributeClass is { } attributeClass &&
            attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace &&
            _mappingAttributeNames.Contains(attributeClass.Name));

    static bool IsCollection(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        // SpecialType is set on the original definition, not on a constructed generic (e.g. IEnumerable<Note>).
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return true;
        }

        return type.AllInterfaces.Any(@interface => @interface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    }

    static INamedTypeSymbol? GetChildElementType(ITypeSymbol? type)
    {
        if (type is null || type.SpecialType == SpecialType.System_String)
        {
            return null;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return named.TypeArguments[0] as INamedTypeSymbol;
        }

        var enumerable = type.AllInterfaces
            .FirstOrDefault(@interface => @interface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        return enumerable?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
    }

    static ITypeSymbol? GetMemberType(ISymbol member) => member switch
    {
        IPropertySymbol property => property.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => null
    };

    static bool IsChildrenFrom(AttributeData attribute) =>
        attribute.AttributeClass is { } attributeClass &&
        string.Equals(attributeClass.Name, ChildrenFromAttributeName, StringComparison.Ordinal) &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;

    static string? ReadIdentifiedBy(AttributeData attribute)
    {
        var constructor = attribute.AttributeConstructor;
        if (constructor is null)
        {
            return null;
        }

        var arguments = attribute.ConstructorArguments;
        for (var i = 0; i < constructor.Parameters.Length && i < arguments.Length; i++)
        {
            if (constructor.Parameters[i].Name == IdentifiedByParameterName)
            {
                return arguments[i].Value as string;
            }
        }

        return null;
    }
}

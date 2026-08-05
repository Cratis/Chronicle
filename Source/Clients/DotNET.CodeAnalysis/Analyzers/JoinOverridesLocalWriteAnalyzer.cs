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
/// Analyzer that reports a model-bound read model member written by both an explicit local mapping
/// (<c>[SetFrom]</c>, <c>[SetValue]</c>, an aggregate, …) and a <c>[Join&lt;TEvent&gt;]</c> — a combination
/// where the joined value always wins and the local write can never reset the property.
/// </summary>
/// <remarks>
/// The fluent equivalent is covered by <see cref="FluentJoinOverridesLocalWriteAnalyzer"/>; both report
/// <see cref="DiagnosticIds.JoinOverridesLocalWrite"/>. AutoMap-driven collisions are the domain of CHR0025.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class JoinOverridesLocalWriteAnalyzer : DiagnosticAnalyzer
{
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string JoinAttributeName = "JoinAttribute";
    const string AttributeSuffix = "Attribute";

    /// <summary>
    /// The attributes that write a member from the read model's own events — the local half of the collision.
    /// </summary>
    static readonly HashSet<string> LocalWriteAttributeNames = new(StringComparer.Ordinal)
    {
        "SetFromAttribute",
        "SetValueAttribute",
        "SetFromContextAttribute",
        "AddFromAttribute",
        "SubtractFromAttribute",
        "CountAttribute",
        "IncrementAttribute",
        "DecrementAttribute"
    };

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(JoinOverridesLocalWrite.Rule);

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

        foreach (var member in GetMergedMembers(typeSymbol))
        {
            AnalyzeMember(context, member);
        }
    }

    static void AnalyzeMember(SymbolAnalysisContext context, (string Name, List<AttributeData> Attributes, Location? Location) member)
    {
        var join = member.Attributes.Find(IsJoin);
        var local = member.Attributes.Find(IsLocalWrite);

        if (join is null || local is null)
        {
            return;
        }

        var joinEventName = ((INamedTypeSymbol)join.AttributeClass!.TypeArguments[0]).Name;
        var localShortName = local.AttributeClass!.Name.Substring(0, local.AttributeClass.Name.Length - AttributeSuffix.Length);
        var localEventName = ((INamedTypeSymbol)local.AttributeClass.TypeArguments[0]).Name;
        var location = join.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? member.Location;

        context.ReportDiagnostic(Diagnostic.Create(
            JoinOverridesLocalWrite.Rule,
            location,
            member.Name,
            $"[{localShortName}<{localEventName}>]",
            joinEventName));
    }

    /// <summary>
    /// Enumerate the members of a type with the attributes of the property and its backing positional record
    /// parameter merged.
    /// </summary>
    /// <param name="typeSymbol">The type to enumerate.</param>
    /// <returns>Each member's name, merged attributes, and a location to fall back to.</returns>
    /// <remarks>
    /// An attribute written without an explicit target on a positional record lands on the primary constructor's
    /// parameter rather than on the generated property. The two colliding attributes usually sit together, but
    /// nothing forces that — targeting one at the property and one at the parameter still collides at runtime —
    /// so the member is judged on the union of both.
    /// </remarks>
    static IEnumerable<(string Name, List<AttributeData> Attributes, Location? Location)> GetMergedMembers(INamedTypeSymbol typeSymbol)
    {
        var members = new Dictionary<string, (List<AttributeData> Attributes, Location? Location)>(StringComparer.Ordinal);

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            Merge(members, property);
        }

        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        foreach (var parameter in primaryConstructor?.Parameters ?? [])
        {
            Merge(members, parameter);
        }

        return members.Select(member => (member.Key, member.Value.Attributes, member.Value.Location));
    }

    static void Merge(Dictionary<string, (List<AttributeData> Attributes, Location? Location)> members, ISymbol symbol)
    {
        if (!members.TryGetValue(symbol.Name, out var entry))
        {
            entry = ([], symbol.Locations.FirstOrDefault());
            members[symbol.Name] = entry;
        }

        entry.Attributes.AddRange(symbol.GetAttributes());
    }

    static bool IsJoin(AttributeData attribute) =>
        IsModelBoundWithEvent(attribute) &&
        string.Equals(attribute.AttributeClass!.Name, JoinAttributeName, StringComparison.Ordinal);

    static bool IsLocalWrite(AttributeData attribute) =>
        IsModelBoundWithEvent(attribute) &&
        LocalWriteAttributeNames.Contains(attribute.AttributeClass!.Name);

    static bool IsModelBoundWithEvent(AttributeData attribute) =>
        attribute.AttributeClass is { TypeArguments.Length: 1 } attributeClass &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace &&
        attributeClass.TypeArguments[0] is INamedTypeSymbol;
}

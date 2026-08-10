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
/// Analyzer that reports a model-bound <c>[Join&lt;TEvent&gt;]</c> copying a <c>[PII]</c> value whose persisted
/// runtime subject cannot be proven to be the read model's compliance subject.
/// </summary>
/// <remarks>
/// The fluent equivalent is covered by <see cref="FluentCrossSubjectPiiJoinAnalyzer"/>; both report
/// <see cref="DiagnosticIds.CrossSubjectPiiJoin"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CrossSubjectPiiJoinAnalyzer : DiagnosticAnalyzer
{
    const string JoinAttributeName = "JoinAttribute";
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string OnParameterName = "on";
    const string EventPropertyNameParameterName = "eventPropertyName";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CrossSubjectPiiJoin.Rule);

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
        var subjectName = CrossSubjectPiiJoin.GetSubjectMemberName(typeSymbol);

        foreach (var (member, attribute) in GetJoinAttributes(typeSymbol))
        {
            AnalyzeJoin(context, member, attribute, subjectName);
        }
    }

    static void AnalyzeJoin(SymbolAnalysisContext context, ISymbol member, AttributeData attribute, string? subjectName)
    {
        if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass ||
            attributeClass.TypeArguments[0] is not INamedTypeSymbol eventType)
        {
            return;
        }

        var (on, eventPropertyName) = ReadArguments(attribute);

        var sourcePropertyName = eventPropertyName ?? member.Name;
        if (!CrossSubjectPiiJoin.IsPii(eventType, sourcePropertyName))
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? member.Locations.FirstOrDefault();
        var joinKey = on ?? subjectName ?? CrossSubjectPiiJoin.IdentifierName;
        context.ReportDiagnostic(Diagnostic.Create(CrossSubjectPiiJoin.Rule, location, member.Name, eventType.Name, sourcePropertyName, joinKey));
    }

    static IEnumerable<(ISymbol Member, AttributeData Attribute)> GetJoinAttributes(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attribute in property.GetAttributes().Where(IsJoin))
            {
                yield return (property, attribute);
            }
        }

        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is null)
        {
            yield break;
        }

        foreach (var parameter in primaryConstructor.Parameters)
        {
            foreach (var attribute in parameter.GetAttributes().Where(IsJoin))
            {
                yield return (parameter, attribute);
            }
        }
    }

    static bool IsJoin(AttributeData attribute) =>
        attribute.AttributeClass is { } attributeClass &&
        string.Equals(attributeClass.Name, JoinAttributeName, StringComparison.Ordinal) &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;

    static (string? On, string? EventPropertyName) ReadArguments(AttributeData attribute)
    {
        var constructor = attribute.AttributeConstructor;
        if (constructor is null)
        {
            return (null, null);
        }

        string? on = null;
        string? eventPropertyName = null;
        var arguments = attribute.ConstructorArguments;

        for (var i = 0; i < constructor.Parameters.Length && i < arguments.Length; i++)
        {
            var value = arguments[i].Value as string;
            switch (constructor.Parameters[i].Name)
            {
                case OnParameterName:
                    on = value;
                    break;

                case EventPropertyNameParameterName:
                    eventPropertyName = value;
                    break;
            }
        }

        return (on, eventPropertyName);
    }
}

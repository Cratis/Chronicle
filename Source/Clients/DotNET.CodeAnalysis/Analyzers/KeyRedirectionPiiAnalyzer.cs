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
/// Analyzer that reports a model-bound <c>[FromEvent&lt;TEvent&gt;(key: …)]</c> or
/// <c>[ChildrenFrom&lt;TEvent&gt;(parentKey: …)]</c> carrying a <c>[PII]</c> value onto a read model whose
/// document is keyed by something other than the stream the event was appended to.
/// </summary>
/// <remarks>
/// The fluent equivalent is covered by <see cref="FluentKeyRedirectionPiiAnalyzer"/>; both report
/// <see cref="DiagnosticIds.KeyRedirectionPii"/>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeyRedirectionPiiAnalyzer : DiagnosticAnalyzer
{
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string FromEventAttributeName = "FromEventAttribute";
    const string ChildrenFromAttributeName = "ChildrenFromAttribute";
    const string SetFromAttributeName = "SetFromAttribute";
    const string KeyParameterName = "key";
    const string ParentKeyParameterName = "parentKey";
    const string ConstantKeyPropertyName = "ConstantKey";
    const string EventPropertyNameParameterName = "eventPropertyName";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(KeyRedirectionPii.Rule);

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

        foreach (var attribute in typeSymbol.GetAttributes().Where(attribute => Is(attribute, FromEventAttributeName)))
        {
            AnalyzeFromEvent(context, typeSymbol, attribute);
        }

        foreach (var (member, memberType, attribute) in GetChildrenFromAttributes(typeSymbol))
        {
            AnalyzeChildrenFrom(context, typeSymbol, member, memberType, attribute);
        }
    }

    static void AnalyzeFromEvent(SymbolAnalysisContext context, INamedTypeSymbol readModelType, AttributeData attribute)
    {
        if (GetEventType(attribute) is not { } eventType ||
            DescribeRedirection(attribute, KeyParameterName, ParentKeyParameterName) is not { } key)
        {
            return;
        }

        Report(context, attribute, eventType, readModelType, readModelType, key);
    }

    static void AnalyzeChildrenFrom(
        SymbolAnalysisContext context,
        INamedTypeSymbol parentReadModelType,
        ISymbol member,
        ITypeSymbol? memberType,
        AttributeData attribute)
    {
        // The child's own key identifies it inside the collection; only the parent key decides which document
        // the collection — and therefore the child's PII — comes to rest on.
        if (GetEventType(attribute) is not { } eventType ||
            DescribeRedirection(attribute, ParentKeyParameterName) is not { } parentKey ||
            GetChildModelType(memberType) is not { } childModelType)
        {
            return;
        }

        Report(context, attribute, eventType, childModelType, parentReadModelType, parentKey);
    }

    static void Report(
        SymbolAnalysisContext context,
        AttributeData attribute,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        INamedTypeSymbol documentReadModelType,
        string key)
    {
        var source = CrossSubjectPiiJoin.FindPiiReachingTheReadModel(
            eventType,
            readModelType,
            GetExplicitMappings(eventType, readModelType),
            !WellKnownTypes.HasAttribute(readModelType, WellKnownTypes.NoAutoMapAttributeName));

        if (source is not { } piiSource)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            KeyRedirectionPii.Rule,
            location,
            piiSource.TargetName,
            eventType.Name,
            piiSource.EventPropertyName,
            key,
            KeyRedirectionPii.ClientReleaseSubjectDescriptionOf(documentReadModelType)));
    }

    /// <summary>
    /// Read the <c>[SetFrom&lt;TEvent&gt;]</c> mappings a read model writes for one event.
    /// </summary>
    /// <param name="eventType">The event the mappings must name.</param>
    /// <param name="readModelType">The read model to read the mappings off.</param>
    /// <returns>Each mapping as the read model property and the event property it takes its value from.</returns>
    static IEnumerable<(string TargetName, string EventPropertyName)> GetExplicitMappings(INamedTypeSymbol eventType, INamedTypeSymbol readModelType) =>
        CrossSubjectPiiJoin.GetMembers(readModelType)
            .SelectMany(member => member.Attributes
                .Where(attribute => Is(attribute, SetFromAttributeName) &&
                                    SymbolEqualityComparer.Default.Equals(GetEventType(attribute), eventType))
                .Select(attribute => (member.Name, ReadArgument(attribute, EventPropertyNameParameterName) ?? member.Name)));

    /// <summary>
    /// Describe the key a model-bound attribute redirects to, when it redirects at all.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="parameterNames">The key parameters that name an event property.</param>
    /// <returns>The key description, or <see langword="null"/> when nothing is redirected.</returns>
    static string? DescribeRedirection(AttributeData attribute, params string[] parameterNames)
    {
        if (parameterNames.Select(name => ReadArgument(attribute, name)).FirstOrDefault(value => value is not null) is { } eventProperty)
        {
            return eventProperty;
        }

        var constantKey = attribute.NamedArguments
            .FirstOrDefault(named => named.Key == ConstantKeyPropertyName)
            .Value.Value as string;

        return string.IsNullOrEmpty(constantKey) ? null : KeyRedirectionPii.DescribeConstantKey(constantKey!);
    }

    static string? ReadArgument(AttributeData attribute, string parameterName)
    {
        var constructor = attribute.AttributeConstructor;
        if (constructor is null)
        {
            return null;
        }

        var arguments = attribute.ConstructorArguments;
        for (var i = 0; i < constructor.Parameters.Length && i < arguments.Length; i++)
        {
            if (constructor.Parameters[i].Name == parameterName && arguments[i].Value is string value && value.Length > 0)
            {
                return value;
            }
        }

        return null;
    }

    static IEnumerable<(ISymbol Member, ITypeSymbol? MemberType, AttributeData Attribute)> GetChildrenFromAttributes(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            foreach (var attribute in property.GetAttributes().Where(attribute => Is(attribute, ChildrenFromAttributeName)))
            {
                yield return (property, property.Type, attribute);
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
            foreach (var attribute in parameter.GetAttributes().Where(attribute => Is(attribute, ChildrenFromAttributeName)))
            {
                yield return (parameter, parameter.Type, attribute);
            }
        }
    }

    static INamedTypeSymbol? GetChildModelType(ITypeSymbol? memberType) =>
        memberType switch
        {
            IArrayTypeSymbol array => array.ElementType as INamedTypeSymbol,
            INamedTypeSymbol { IsGenericType: true } named => named.TypeArguments.FirstOrDefault() as INamedTypeSymbol,
            _ => null
        };

    static INamedTypeSymbol? GetEventType(AttributeData attribute) =>
        attribute.AttributeClass is { TypeArguments.Length: 1 } attributeClass
            ? attributeClass.TypeArguments[0] as INamedTypeSymbol
            : null;

    static bool Is(AttributeData attribute, string attributeName) =>
        attribute.AttributeClass is { } attributeClass &&
        string.Equals(attributeClass.Name, attributeName, StringComparison.Ordinal) &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;
}

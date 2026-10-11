// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders relationship annotations on model-bound records and their members.
/// </summary>
static class ModelBoundRelationships
{
    /// <summary>
    /// Builds subscriptions and removal annotations for a record.
    /// </summary>
    /// <param name="definition">The projection scope to render.</param>
    /// <param name="isCollection">Whether subscriptions are supplied by the enclosing collection.</param>
    /// <returns>The record annotations.</returns>
    internal static IEnumerable<AttributeSyntax> ClassAttributes(ProjectionDefinition definition, bool isCollection) => isCollection
        ? []
        : definition.From.Select(from => Create($"FromEvent<{from.Key.Id.Value}>", KeyArguments(from.Key.Id, from.Value.Key, from.Value.ParentKey)))
            .Concat(RemovalAttributes(definition));

    /// <summary>
    /// Builds annotations for joins, child collections and nested objects.
    /// </summary>
    /// <param name="definition">The enclosing projection scope.</param>
    /// <param name="property">The target property.</param>
    /// <param name="childType">The type of a child or nested object.</param>
    /// <param name="isCollection">Whether the enclosing scope is a child collection.</param>
    /// <returns>The member annotations.</returns>
    internal static IEnumerable<AttributeSyntax> PropertyAttributes(ProjectionDefinition definition, PropertyPath property, TypeSyntax childType, bool isCollection)
    {
        var childIdentity = isCollection && definition is ChildrenDefinition children ? children.IdentifiedBy : null;
        var member = new Member(property, childType, childIdentity);
        return JoinAttributes(definition, member).Concat(ChildAttributes(definition, member)).Concat(NestedAttributes(definition, property));
    }

    static IEnumerable<AttributeSyntax> JoinAttributes(ProjectionDefinition definition, Member member) => definition.Join
        .Where(join => join.Value.Properties.ContainsKey(member.Target)).Select(join => JoinAttribute(join, member));

    static AttributeSyntax JoinAttribute(KeyValuePair<EventType, JoinDefinition> join, Member member)
    {
        var arguments = new List<string>();
        if (!join.Value.On.Equals(member.ChildIdentity)) arguments.Add($"on: \"{join.Value.On.Path}\"");
        arguments.Add($"eventPropertyName: nameof({join.Key.Id.Value}.{join.Value.Properties[member.Target]})");
        return Create($"Join<{join.Key.Id.Value}>", arguments);
    }

    static IEnumerable<AttributeSyntax> ChildAttributes(ProjectionDefinition definition, Member member)
    {
        if (!definition.Children.TryGetValue(member.Target, out var children))
        {
            return [];
        }
        return children.From.Select(from => ChildAttribute(from, member, children.IdentifiedBy)).Concat(RemovalAttributes(children));
    }

    static AttributeSyntax ChildAttribute(KeyValuePair<EventType, FromDefinition> from, Member member, PropertyPath identifiedBy)
    {
        var arguments = KeyArguments(from.Key.Id, from.Value.Key, from.Value.ParentKey).ToList();
        arguments.Add($"identifiedBy: nameof({member.ChildType}.{identifiedBy.Path})");
        if (string.IsNullOrEmpty(from.Value.ParentKey?.Value)) arguments.Add("parentKey: \"$eventSourceId\"");
        return Create($"ChildrenFrom<{from.Key.Id.Value}>", arguments);
    }

    static IEnumerable<AttributeSyntax> NestedAttributes(ProjectionDefinition definition, PropertyPath property)
    {
        if (definition.Nested?.TryGetValue(property, out var nested) == true)
        {
            return new[] { Attribute(IdentifierName("Nested")) }.Concat(nested.RemovedWith.Keys.Select(eventType => Create($"ClearWith<{eventType.Id.Value}>", [])));
        }
        return [];
    }

    static IEnumerable<AttributeSyntax> RemovalAttributes(ProjectionDefinition definition) => definition.RemovedWith
        .Select(removed => Create($"RemovedWith<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id, removed.Value.Key, removed.Value.ParentKey)))
        .Concat(definition.RemovedWithJoin.Select(removed => Create($"RemovedWithJoin<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id, removed.Value.Key, null))));

    static IEnumerable<string> KeyArguments(EventTypeId eventType, PropertyExpression key, PropertyExpression? parentKey)
    {
        if (!CSharpDefinitionSupport.IsDefaultKey(key)) yield return $"key: {KeyValue(eventType, key)}";
        if (!string.IsNullOrEmpty(parentKey?.Value)) yield return $"parentKey: {KeyValue(eventType, parentKey)}";
    }

    static string KeyValue(EventTypeId eventType, PropertyExpression expression)
    {
        var key = CSharpDefinitionSupport.AttributeKey(expression);
        return key.StartsWith('$') ? $"\"{key}\"" : $"nameof({eventType.Value}.{key})";
    }

    static AttributeSyntax Create(string name, IEnumerable<string> arguments)
    {
        var attribute = Attribute(ParseName(name));
        var parsed = arguments.Select(argument =>
        {
            var parts = argument.Split(": ", 2, StringSplitOptions.None);
            return AttributeArgument(ParseExpression(parts[1])).WithNameColon(NameColon(IdentifierName(parts[0])));
        }).ToArray();
        return parsed.Length == 0 ? attribute : attribute.WithArgumentList(AttributeArgumentList(SeparatedList(parsed)));
    }

    sealed record Member(PropertyPath Target, TypeSyntax ChildType, PropertyPath? ChildIdentity);
}

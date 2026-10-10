// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    internal static IEnumerable<AttributeSyntax> ClassAttributes(ProjectionDefinition definition, bool isCollection)
    {
        if (!isCollection)
        {
            foreach (var from in definition.From)
            {
                yield return Create($"FromEvent<{from.Key.Id.Value}>", KeyArguments(from.Key.Id.Value, from.Value.Key, from.Value.ParentKey));
            }

            foreach (var removed in definition.RemovedWith)
            {
                yield return Create($"RemovedWith<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id.Value, removed.Value.Key, removed.Value.ParentKey));
            }

            foreach (var removed in definition.RemovedWithJoin)
            {
                yield return Create($"RemovedWithJoin<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id.Value, removed.Value.Key, null));
            }
        }
    }

    /// <summary>
    /// Builds annotations for joins, child collections and nested objects.
    /// </summary>
    /// <param name="definition">The enclosing projection scope.</param>
    /// <param name="property">The target property.</param>
    /// <param name="childType">The type of a child or nested object.</param>
    /// <param name="isCollection">Whether the enclosing scope is a child collection.</param>
    /// <returns>The member annotations.</returns>
    internal static IEnumerable<AttributeSyntax> PropertyAttributes(ProjectionDefinition definition, string property, string childType, bool isCollection)
    {
        foreach (var join in definition.Join.Where(join => join.Value.Properties.ContainsKey(new PropertyPath(property))))
        {
            var arguments = new List<string>();
            if (!isCollection)
            {
                arguments.Add($"on: \"{join.Value.On.Path}\"");
            }
            var source = join.Value.Properties[new PropertyPath(property)];
            arguments.Add($"eventPropertyName: nameof({join.Key.Id.Value}.{source})");
            yield return Create($"Join<{join.Key.Id.Value}>", arguments);
        }

        if (definition.Children.TryGetValue(new PropertyPath(property), out var children))
        {
            foreach (var from in children.From)
            {
                var arguments = KeyArguments(from.Key.Id.Value, from.Value.Key, from.Value.ParentKey).ToList();
                arguments.Add($"identifiedBy: nameof({childType}.{children.IdentifiedBy.Path})");
                if (from.Value.ParentKey is null || string.IsNullOrEmpty(from.Value.ParentKey.Value))
                {
                    arguments.Add("parentKey: \"$eventSourceId\"");
                }
                yield return Create($"ChildrenFrom<{from.Key.Id.Value}>", arguments);
            }

            foreach (var removed in children.RemovedWith)
            {
                yield return Create($"RemovedWith<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id.Value, removed.Value.Key, removed.Value.ParentKey));
            }

            foreach (var removed in children.RemovedWithJoin)
            {
                yield return Create($"RemovedWithJoin<{removed.Key.Id.Value}>", KeyArguments(removed.Key.Id.Value, removed.Value.Key, null));
            }
        }

        if (definition.Nested?.TryGetValue(new PropertyPath(property), out var nested) == true)
        {
            yield return Attribute(IdentifierName("Nested"));
            foreach (var removed in nested.RemovedWith)
            {
                yield return Create($"ClearWith<{removed.Key.Id.Value}>", []);
            }
        }
    }

    static IEnumerable<string> KeyArguments(string eventType, string key, PropertyExpression? parentKey)
    {
        if (!CSharpDefinitionSupport.IsDefaultKey(key))
        {
            yield return $"key: {KeyValue(eventType, key)}";
        }
        if (!string.IsNullOrEmpty(parentKey?.Value))
        {
            yield return $"parentKey: {KeyValue(eventType, parentKey.Value)}";
        }
    }

    static string KeyValue(string eventType, string expression)
    {
        var key = CSharpDefinitionSupport.AttributeKey(expression);
        return key.StartsWith('$') ? $"\"{key}\"" : $"nameof({eventType}.{key})";
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
}

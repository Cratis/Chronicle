// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Collects the mappings rendered by the model-bound code generator.
/// </summary>
public partial class ModelBoundCodeGenerator
{
    enum CallKind
    {
        Add = 0,
        Subtract = 1,
        Value = 2
    }

    static Dictionary<string, PropertyInfo> CollectPropertyMappings(ProjectionDefinition definition)
    {
        var properties = new Dictionary<string, PropertyInfo>();
        foreach (var from in definition.From)
        {
            CollectEventMappings(from, properties);
        }
        CollectEveryMappings(definition, properties);
        return properties;
    }

    static void CollectEventMappings(KeyValuePair<EventType, FromDefinition> from, Dictionary<string, PropertyInfo> properties)
    {
        foreach (var mapping in from.Value.Properties)
        {
            var expression = new PropertyExpression(DeclarativeExpressions.Normalize(mapping.Value));
            AddEventMapping(GetPropertyInfo(properties, mapping.Key), from.Key.Id, expression);
        }
    }

    static PropertyInfo GetPropertyInfo(Dictionary<string, PropertyInfo> properties, PropertyPath target)
    {
        if (!properties.TryGetValue(target.Path, out var property))
        {
            property = new PropertyInfo();
            properties[target.Path] = property;
        }
        return property;
    }

    static void AddEventMapping(PropertyInfo property, EventTypeId eventType, PropertyExpression expression)
    {
        if (ReadCall(expression) is { } call)
        {
            AddCallMapping(property, eventType, call);
            return;
        }
        switch (expression.Value)
        {
            case WellKnownExpressions.Increment: property.Increments.Add(eventType.Value); break;
            case WellKnownExpressions.Decrement: property.Decrements.Add(eventType.Value); break;
            case WellKnownExpressions.Count: property.Counts.Add(eventType.Value); break;
            case WellKnownExpressions.Null: property.Clears.Add(eventType.Value); break;
            default: AddSetMapping(property, eventType, expression); break;
        }
    }

    static void AddCallMapping(PropertyInfo property, EventTypeId eventType, ExpressionCall call)
    {
        switch (call.Kind)
        {
            case CallKind.Add: property.AddFroms.Add((eventType.Value, call.Argument)); break;
            case CallKind.Subtract: property.SubtractFroms.Add((eventType.Value, call.Argument)); break;
            case CallKind.Value: property.SetValues.Add((eventType.Value, call.Argument, false)); break;
        }
    }

    static void AddSetMapping(PropertyInfo property, EventTypeId eventType, PropertyExpression expression)
    {
        var source = ProjectionExpressions.ReadValue(expression);
        if (source.Kind is ProjectionValueKind.Text or ProjectionValueKind.Literal)
        {
            property.SetValues.Add((eventType.Value, source.Value, source.Kind == ProjectionValueKind.Text));
        }
        else
        {
            property.SetFroms.Add((eventType.Value, expression.Value));
        }
    }

    static void CollectEveryMappings(ProjectionDefinition definition, Dictionary<string, PropertyInfo> properties)
    {
        var mappings = definition.FromEvery.Properties
            .Select(mapping => new PropertyMapping(mapping.Key, DeclarativeExpressions.Normalize(mapping.Value)))
            .Where(mapping => ModelBoundLimitations.CanMapEvery(definition, mapping.Target.Path, mapping.Expression));
        foreach (var mapping in mappings)
        {
            AddEveryMapping(GetPropertyInfo(properties, mapping.Target), definition, mapping.Expression);
        }
    }

    static void AddEveryMapping(PropertyInfo property, ProjectionDefinition definition, PropertyExpression expression)
    {
        // CanMapEvery admits only context mappings in child scopes. Expand those over local subscriptions.
        if (definition.Owner == ProjectionOwner.Parent)
        {
            property.SetFroms.AddRange(definition.From.Keys.Select(eventType => (eventType.Id.Value, expression.Value)));
            return;
        }
        switch (expression.Value)
        {
            case WellKnownExpressions.Count: property.FromAllCount = true; break;
            case WellKnownExpressions.Increment: property.FromAllIncrement = true; break;
            case WellKnownExpressions.Decrement: property.FromAllDecrement = true; break;
            default: AddEverySource(property, expression); break;
        }
    }

    static void AddEverySource(PropertyInfo property, PropertyExpression expression)
    {
        if (ReadArgument(expression, WellKnownExpressions.EventContext) is { } context)
        {
            property.FromAllContextProperty = CSharpDefinitionSupport.ContextPath(context);
        }
        else
        {
            property.FromAllProperty = expression.Value;
        }
    }

    static ExpressionCall? ReadCall(PropertyExpression expression) => new[] { CallKind.Add, CallKind.Subtract, CallKind.Value }
        .Select(kind => ParseCall(expression, kind)).FirstOrDefault(call => call is not null);

    static ExpressionCall? ParseCall(PropertyExpression expression, CallKind kind)
    {
        var operation = kind switch
        {
            CallKind.Add => WellKnownExpressions.Add,
            CallKind.Subtract => WellKnownExpressions.Subtract,
            _ => WellKnownExpressions.Value
        };
        return ReadArgument(expression, operation) is { } argument ? new ExpressionCall(kind, argument) : null;
    }

    static string? ReadArgument(PropertyExpression expression, PropertyExpression operation)
    {
        var prefix = $"{operation.Value}(";
        return expression.Value.StartsWith(prefix, StringComparison.Ordinal) && expression.Value.EndsWith(')')
            ? expression.Value[prefix.Length..^1]
            : null;
    }

    sealed record PropertyMapping(PropertyPath Target, PropertyExpression Expression);

    sealed record ExpressionCall(CallKind Kind, string Argument);
}

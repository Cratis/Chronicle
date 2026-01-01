// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.DSL.AST;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Compiles an AST Document from the RulesProjectionDslParser into a ProjectionDefinition.
/// </summary>
/// <remarks>
/// This is a work in progress. Currently implements basic event handling and property mappings.
/// TODO: Implement children operations, parent relationships, joins, and advanced features.
/// </remarks>
public class AstToProjectionDefinitionCompiler
{
    /// <summary>
    /// Compiles an AST Document into a ProjectionDefinition.
    /// </summary>
    /// <param name="document">The AST document to compile.</param>
    /// <param name="identifier">The projection identifier.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>A ProjectionDefinition.</returns>
    public ProjectionDefinition Compile(
        Document document,
        ProjectionId identifier,
        ProjectionOwner owner,
        EventSequenceId eventSequenceId)
    {
        if (document.Projections.Count == 0)
        {
            throw new InvalidOperationException("Document must contain at least one projection");
        }

        // For now, compile the first projection
        var projection = document.Projections[0];

        var from = new Dictionary<EventType, FromDefinition>();
        var join = new Dictionary<EventType, JoinDefinition>();
        var children = new Dictionary<PropertyPath, ChildrenDefinition>();
        var removedWith = new Dictionary<EventType, RemovedWithDefinition>();
        var removedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>();
        var fromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false);

        // Process each directive
        foreach (var directive in projection.Directives)
        {
            ProcessDirective(directive, from, join, children, removedWith, removedWithJoin, ref fromEvery);
        }

        return new ProjectionDefinition(
            owner,
            eventSequenceId,
            identifier,
            new ReadModelIdentifier(projection.ReadModelType.Name),
            IsActive: true,
            IsRewindable: false,
            new JsonObject(),
            from,
            join,
            children,
            [],
            fromEvery,
            removedWith,
            removedWithJoin,
            FromEventProperty: null,
            LastUpdated: DateTimeOffset.UtcNow);
    }

    void ProcessDirective(
        ProjectionDirective directive,
        Dictionary<EventType, FromDefinition> from,
        Dictionary<EventType, JoinDefinition> join,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith,
        Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin,
        ref FromEveryDefinition fromEvery)
    {
        switch (directive)
        {
            case OnEventBlock onEvent:
                ProcessOnEventBlock(onEvent, from);
                break;
            case AutoMapDirective:
                // TODO: Implement automap
                break;
            case ChildrenBlock childrenBlock:
                ProcessChildrenBlock(childrenBlock, children, removedWith);
                break;
            case JoinBlock joinBlock:
                ProcessJoinBlock(joinBlock, join);
                break;
            default:
                throw new NotSupportedException($"Directive type {directive.GetType().Name} is not yet supported");
        }
    }

    void ProcessOnEventBlock(OnEventBlock onEvent, Dictionary<EventType, FromDefinition> from)
    {
        var eventType = EventType.Parse(onEvent.EventType.Name);
        var properties = new Dictionary<PropertyPath, string>();
        var keyExpression = onEvent.Key != null ? ConvertExpression(onEvent.Key) : PropertyExpression.NotSet;

        foreach (var operation in onEvent.Mappings)
        {
            switch (operation)
            {
                case AssignmentOperation assignment:
                    var propertyPath = new PropertyPath(assignment.PropertyName);
                    properties[propertyPath] = ConvertExpressionToString(assignment.Value);
                    break;
                case AddOperation add:
                    var addPath = new PropertyPath(add.PropertyName);
                    properties[addPath] = $"+= {ConvertExpressionToString(add.Value)}";
                    break;
                case SubtractOperation subtract:
                    var subtractPath = new PropertyPath(subtract.PropertyName);
                    properties[subtractPath] = $"-= {ConvertExpressionToString(subtract.Value)}";
                    break;
                case IncrementOperation increment:
                    var incrementPath = new PropertyPath(increment.PropertyName);
                    properties[incrementPath] = "increment";
                    break;
                case DecrementOperation decrement:
                    var decrementPath = new PropertyPath(decrement.PropertyName);
                    properties[decrementPath] = "decrement";
                    break;
                case CountOperation count:
                    var countPath = new PropertyPath(count.PropertyName);
                    properties[countPath] = "count";
                    break;
                default:
                    throw new NotSupportedException($"Operation type {operation.GetType().Name} is not yet supported");
            }
        }

        from[eventType] = new FromDefinition(properties, keyExpression, ParentKey: null);
    }

    void ProcessChildrenBlock(
        ChildrenBlock childrenBlock,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        // TODO: Implement children block processing
        // This requires understanding the children definitions structure
    }

    void ProcessJoinBlock(JoinBlock joinBlock, Dictionary<EventType, JoinDefinition> join)
    {
        // TODO: Implement join block processing
        // This requires understanding the join definitions structure
    }

    PropertyExpression ConvertExpression(AST.Expression astExpression)
    {
        return astExpression switch
        {
            EventDataExpression eventData => new PropertyExpression(eventData.Path),
            EventContextExpression eventContext => new PropertyExpression($"$eventContext.{eventContext.Property}"),
            EventSourceIdExpression => new PropertyExpression("$eventSourceId"),
            LiteralExpression literal => new PropertyExpression(literal.Value?.ToString() ?? string.Empty),
            TemplateExpression template => new PropertyExpression(ConvertTemplateToString(template)),
            _ => throw new NotSupportedException($"Expression type {astExpression.GetType().Name} is not yet supported")
        };
    }

    string ConvertExpressionToString(AST.Expression astExpression)
    {
        return astExpression switch
        {
            EventDataExpression eventData => eventData.Path,
            EventContextExpression eventContext => $"$eventContext.{eventContext.Property}",
            EventSourceIdExpression => "$eventSourceId",
            LiteralExpression literal => literal.Value?.ToString() ?? string.Empty,
            TemplateExpression template => ConvertTemplateToString(template),
            _ => throw new NotSupportedException($"Expression type {astExpression.GetType().Name} is not yet supported")
        };
    }

    string ConvertTemplateToString(TemplateExpression template)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var part in template.Parts)
        {
            switch (part)
            {
                case TemplateTextPart text:
                    sb.Append(text.Text);
                    break;
                case TemplateExpressionPart expr:
                    sb.Append("${");
                    sb.Append(ConvertExpressionToString(expr.Expression));
                    sb.Append('}');
                    break;
            }
        }
        return $"`{sb}`";
    }
}

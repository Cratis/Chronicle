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
/// Compiles an AST Document representing the projection into a ProjectionDefinition.
/// </summary>
public class Compiler
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
        document.Validate();

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
            case FromEventBlock onEvent:
                ProcessOnEventBlock(onEvent, from);
                break;
            case EveryBlock every:
                fromEvery = ProcessEveryBlock(every);
                break;
            case KeyDirective:
            case CompositeKeyDirective:
                // Keys are handled within OnEventBlocks
                break;
            case AutoMapDirective:
                // AutoMap is handled within OnEventBlocks
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

    void ProcessOnEventBlock(FromEventBlock onEvent, Dictionary<EventType, FromDefinition> from)
    {
        var eventType = EventType.Parse(onEvent.EventType.Name);
        var properties = new Dictionary<PropertyPath, string>();
        var keyExpression = onEvent.Key != null ? ConvertExpression(onEvent.Key) : PropertyExpression.NotSet;
        var parentKeyExpression = onEvent.ParentKey != null ? ConvertExpression(onEvent.ParentKey) : null;

        foreach (var operation in onEvent.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        from[eventType] = new FromDefinition(properties, keyExpression, parentKeyExpression)
        {
            AutoMap = onEvent.AutoMap ? AutoMap.Enabled : AutoMap.Inherit
        };
    }

    void ProcessChildrenBlock(
        ChildrenBlock childrenBlock,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        var collectionPath = new PropertyPath(childrenBlock.CollectionName);
        var identifiedBy = ConvertExpression(childrenBlock.IdentifierExpression);

        var childFrom = new Dictionary<EventType, FromDefinition>();
        var childJoin = new Dictionary<EventType, JoinDefinition>();
        var nestedChildren = new Dictionary<PropertyPath, ChildrenDefinition>();
        var childRemovedWith = new Dictionary<EventType, RemovedWithDefinition>();
        var childRemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>();
        var childEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false);

        foreach (var childBlock in childrenBlock.ChildBlocks)
        {
            ProcessChildBlock(childBlock, childFrom, childJoin, nestedChildren, childRemovedWith, childRemovedWithJoin, ref childEvery);
        }

        children[collectionPath] = new ChildrenDefinition(
            new PropertyPath(identifiedBy.Value),
            childFrom,
            childJoin,
            nestedChildren,
            childEvery,
            childRemovedWith,
            childRemovedWithJoin)
        {
            AutoMap = childrenBlock.AutoMap ? AutoMap.Enabled : AutoMap.Inherit
        };
    }

    void ProcessChildBlock(
        ChildBlock childBlock,
        Dictionary<EventType, FromDefinition> from,
        Dictionary<EventType, JoinDefinition> join,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith,
        Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin,
        ref FromEveryDefinition fromEvery)
    {
        switch (childBlock)
        {
            case ChildOnEventBlock onEvent:
                ProcessChildOnEventBlock(onEvent, from);
                break;
            case ChildJoinBlock joinBlock:
                ProcessChildJoinBlock(joinBlock, join);
                break;
            case NestedChildrenBlock nestedChildren:
                ProcessNestedChildrenBlock(nestedChildren, children, removedWith);
                break;
            case RemoveBlock remove:
                ProcessRemoveBlock(remove, removedWith);
                break;
            case RemoveViaJoinBlock removeJoin:
                ProcessRemoveViaJoinBlock(removeJoin, removedWithJoin);
                break;
            default:
                throw new NotSupportedException($"Child block type {childBlock.GetType().Name} is not yet supported");
        }
    }

    void ProcessChildOnEventBlock(ChildOnEventBlock onEvent, Dictionary<EventType, FromDefinition> from)
    {
        var eventType = EventType.Parse(onEvent.EventType.Name);
        var properties = new Dictionary<PropertyPath, string>();
        var keyExpression = onEvent.Key != null ? ConvertExpression(onEvent.Key) : PropertyExpression.NotSet;
        var parentKeyExpression = onEvent.ParentKey != null ? ConvertExpression(onEvent.ParentKey) : null;

        foreach (var operation in onEvent.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        from[eventType] = new FromDefinition(properties, keyExpression, parentKeyExpression);
    }

    void ProcessChildJoinBlock(ChildJoinBlock joinBlock, Dictionary<EventType, JoinDefinition> join)
    {
        var properties = new Dictionary<PropertyPath, string>();

        foreach (var operation in joinBlock.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        // For joins, we need to handle each event type
        foreach (var eventType in joinBlock.EventTypes)
        {
            var et = EventType.Parse(eventType.Name);
            join[et] = new JoinDefinition(
                new PropertyPath(joinBlock.OnProperty),
                properties,
                PropertyExpression.NotSet);
        }
    }

    void ProcessNestedChildrenBlock(
        NestedChildrenBlock nestedChildren,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        // Convert to ChildrenBlock and process
        var childrenBlock = new ChildrenBlock(
            nestedChildren.CollectionName,
            nestedChildren.IdentifierExpression,
            nestedChildren.AutoMap,
            nestedChildren.ChildBlocks);

        ProcessChildrenBlock(childrenBlock, children, removedWith);
    }

    void ProcessRemoveBlock(RemoveBlock remove, Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        var eventType = EventType.Parse(remove.EventType.Name);
        var key = remove.Key != null ? ConvertExpression(remove.Key) : PropertyExpression.NotSet;
        var parentKey = remove.ParentKey != null ? ConvertExpression(remove.ParentKey) : null;

        removedWith[eventType] = new RemovedWithDefinition(key, parentKey);
    }

    void ProcessRemoveViaJoinBlock(RemoveViaJoinBlock removeJoin, Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin)
    {
        var eventType = EventType.Parse(removeJoin.EventType.Name);
        var key = removeJoin.Key != null ? ConvertExpression(removeJoin.Key) : PropertyExpression.NotSet;

        removedWithJoin[eventType] = new RemovedWithJoinDefinition(key);
    }

    void ProcessJoinBlock(JoinBlock joinBlock, Dictionary<EventType, JoinDefinition> join)
    {
        var properties = new Dictionary<PropertyPath, string>();

        foreach (var operation in joinBlock.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        // For joins, we need to handle each event type
        foreach (var eventType in joinBlock.EventTypes)
        {
            var et = EventType.Parse(eventType.Name);
            join[et] = new JoinDefinition(
                new PropertyPath(joinBlock.OnProperty),
                properties,
                PropertyExpression.NotSet)
            {
                AutoMap = joinBlock.AutoMap ? AutoMap.Enabled : AutoMap.Inherit
            };
        }
    }

    FromEveryDefinition ProcessEveryBlock(EveryBlock every)
    {
        var properties = new Dictionary<PropertyPath, string>();

        foreach (var operation in every.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        return new FromEveryDefinition(properties, !every.ExcludeChildren)
        {
            AutoMap = every.AutoMap ? AutoMap.Enabled : AutoMap.Inherit
        };
    }

    void ProcessMappingOperation(MappingOperation operation, Dictionary<PropertyPath, string> properties)
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

    PropertyExpression ConvertExpression(Expression astExpression)
    {
        return astExpression switch
        {
            EventDataExpression eventData => new PropertyExpression(eventData.Path),
            EventContextExpression eventContext => new PropertyExpression($"$eventContext({eventContext.Property})"),
            EventSourceIdExpression => new PropertyExpression("$eventSourceId"),
            LiteralExpression literal => new PropertyExpression(literal.Value?.ToString() ?? string.Empty),
            TemplateExpression template => new PropertyExpression(ConvertTemplateToString(template)),
            _ => throw new NotSupportedException($"Expression type {astExpression.GetType().Name} is not yet supported")
        };
    }

    string ConvertExpressionToString(Expression astExpression)
    {
        return astExpression switch
        {
            EventDataExpression eventData => eventData.Path,
            EventContextExpression eventContext => $"$eventContext({eventContext.Property})",
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
                    sb
                        .Append("${")
                        .Append(ConvertExpressionToString(expr.Expression))
                        .Append('}');
                    break;
            }
        }
        return $"`{sb}`";
    }
}

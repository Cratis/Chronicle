// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Compiles an AST Document representing the projection into a ProjectionDefinition.
/// </summary>
public class Compiler
{
    bool _hasNoAutoMapDirective;

    /// <summary>
    /// Gets the read model identifier from an AST Document.
    /// </summary>
    /// <param name="document">The AST document to extract from.</param>
    /// <returns>The read model identifier.</returns>
    public Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(Document document)
    {
        var validationResult = document.Validate();
        if (!validationResult.IsSuccess)
        {
            return CompilerErrors.From(validationResult.AsT1);
        }

        var projection = document.Projections[0];
        return new ReadModelIdentifier(projection.ReadModelType.Name);
    }

    /// <summary>
    /// Compiles an AST Document into a ProjectionDefinition.
    /// </summary>
    /// <param name="document">The AST document to compile.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="readModelDefinitions">Available read model definitions for validation.</param>
    /// <param name="eventTypeSchemas">Available event type schemas for validation.</param>
    /// <returns>A ProjectionDefinition or compiler errors.</returns>
    public Result<ProjectionDefinition, CompilerErrors> Compile(
        Document document,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        var documentValidationResult = document.Validate();
        if (!documentValidationResult.IsSuccess)
        {
            return CompilerErrors.From(documentValidationResult.AsT1);
        }

        // For now, compile the first projection
        var projection = document.Projections[0];
        var validationResult = projection.Validate();
        if (!validationResult.IsSuccess)
        {
            return CompilerErrors.From(validationResult.AsT1);
        }

        var errors = new CompilerErrors();

        // Validate the projection if schemas are provided
        if (readModelDefinitions.Any() || eventTypeSchemas.Any())
        {
            var validator = new ProjectionValidator(readModelDefinitions, eventTypeSchemas);
            validator.Validate(projection, errors);

            if (errors.HasErrors)
            {
                return errors;
            }
        }

        var identifier = new ProjectionId(projection.Name);

        // Check if NoAutoMapDirective is present in the projection
        _hasNoAutoMapDirective = projection.Directives.OfType<NoAutoMapDirective>().Any();

        // Extract event sequence ID from sequence directive, or default to Log
        var eventSequenceId = EventSequenceId.Log;
        var sequenceDirective = projection.Directives.OfType<SequenceDirective>().FirstOrDefault();
        if (sequenceDirective is not null)
        {
            eventSequenceId = new EventSequenceId(sequenceDirective.SequenceId);
        }

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
            LastUpdated: DateTimeOffset.UtcNow,
            Tags: default,
            AutoMap: _hasNoAutoMapDirective ? AutoMap.Disabled : AutoMap.Enabled);
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
            case MultiFromEventBlock multiFromEvent:
                // Expand multiple from blocks into individual blocks
                foreach (var block in multiFromEvent.Blocks)
                {
                    ProcessOnEventBlock(block, from);
                }
                break;
            case EveryBlock every:
                fromEvery = ProcessEveryBlock(every);
                break;
            case SequenceDirective:
                // Sequence is handled before processing directives
                break;
            case KeyDirective:
            case CompositeKeyDirective:
                // Keys are handled within OnEventBlocks
                break;
            case AutoMapDirective:
                // AutoMap is handled within OnEventBlocks
                break;
            case NoAutoMapDirective:
                // NoAutoMap is tracked in _hasNoAutoMapDirective
                break;
            case ChildrenBlock childrenBlock:
                ProcessChildrenBlock(childrenBlock, children);
                break;
            case JoinBlock joinBlock:
                ProcessJoinBlock(joinBlock, join);
                break;
            case RemoveWithDirective removeWith:
                ProcessRemoveWithDirective(removeWith, removedWith);
                break;
            case RemoveWithJoinDirective removeWithJoin:
                ProcessRemoveWithJoinDirective(removeWithJoin, removedWithJoin);
                break;
            default:
                throw new NotSupportedException($"Directive type {directive.GetType().Name} is not yet supported");
        }
    }

    AutoMap GetAutoMapValue(AutoMap blockAutoMap)
    {
        // If block has explicit directive (Enabled or Disabled), use it
        if (blockAutoMap != AutoMap.Inherit)
        {
            return blockAutoMap;
        }

        // Otherwise check projection-level directive
        // If NoAutoMapDirective is present, default is Disabled (opt-out)
        // Otherwise, default is Enabled (auto-map by default)
        return _hasNoAutoMapDirective ? AutoMap.Disabled : AutoMap.Enabled;
    }

    void ProcessOnEventBlock(FromEventBlock onEvent, Dictionary<EventType, FromDefinition> from)
    {
        var eventType = EventType.Parse(onEvent.EventType.Name);
        var properties = new Dictionary<PropertyPath, string>();

        PropertyExpression keyExpression;
        if (onEvent.CompositeKey != null)
        {
            // Build composite key expression: $composite(TypeName, prop1=expr1, prop2=expr2, ...)
            var typeName = onEvent.CompositeKey.TypeName.Name;
            var parts = string.Join(", ", onEvent.CompositeKey.Parts.Select(p => $"{p.PropertyName}={ConvertExpression(p.Expression).Value}"));
            keyExpression = new PropertyExpression($"{WellKnownExpressions.Composite}({typeName}, {parts})");
        }
        else if (onEvent.Key != null)
        {
            keyExpression = ConvertExpression(onEvent.Key);
        }
        else
        {
            keyExpression = PropertyExpression.NotSet;
        }

        var parentKeyExpression = onEvent.ParentKey != null ? ConvertExpression(onEvent.ParentKey) : null;

        foreach (var operation in onEvent.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        from[eventType] = new FromDefinition(properties, keyExpression, parentKeyExpression);
    }

    void ProcessChildrenBlock(
        ChildrenBlock childrenBlock,
        Dictionary<PropertyPath, ChildrenDefinition> children)
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
            ProcessChildBlock(childBlock, childFrom, childJoin, nestedChildren, childRemovedWith, childRemovedWithJoin, childEvery);
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
            AutoMap = GetAutoMapValue(childrenBlock.AutoMap)
        };
    }

    /// <summary>
    /// Process a child block and populate the appropriate dictionaries based on its type.
    /// Children support 'every' blocks in the DSL for mapping all events to properties.
    /// </summary>
    /// <param name="childBlock">The child block to process.</param>
    /// <param name="from">Dictionary of event type to from definitions.</param>
    /// <param name="join">Dictionary of event type to join definitions.</param>
    /// <param name="children">Dictionary of property path to children definitions.</param>
    /// <param name="removedWith">Dictionary of event type to removed with definitions.</param>
    /// <param name="removedWithJoin">Dictionary of event type to removed with join definitions.</param>
    /// <param name="fromEvery">The from every definition to populate when processing every blocks.</param>
    /// <exception cref="NotSupportedException">Thrown when the child block type is not yet supported.</exception>
    void ProcessChildBlock(
        ChildBlock childBlock,
        Dictionary<EventType, FromDefinition> from,
        Dictionary<EventType, JoinDefinition> join,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith,
        Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin,
        FromEveryDefinition fromEvery)
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
                ProcessNestedChildrenBlock(nestedChildren, children);
                break;
            case RemoveBlock remove:
                ProcessRemoveBlock(remove, removedWith);
                break;
            case RemoveViaJoinBlock removeJoin:
                ProcessRemoveViaJoinBlock(removeJoin, removedWithJoin);
                break;
            case ChildEveryBlock everyBlock:
                ProcessChildEveryBlock(everyBlock, fromEvery);
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
        // Each 'with' block creates a separate join definition
        foreach (var withBlock in joinBlock.WithBlocks)
        {
            var properties = new Dictionary<PropertyPath, string>();

            foreach (var operation in withBlock.Mappings)
            {
                ProcessMappingOperation(operation, properties);
            }

            var eventType = EventType.Parse(withBlock.EventType.Name);
            join[eventType] = new JoinDefinition(
                new PropertyPath(joinBlock.OnProperty),
                properties,
                PropertyExpression.NotSet);
        }
    }

    void ProcessNestedChildrenBlock(
        NestedChildrenBlock nestedChildren,
        Dictionary<PropertyPath, ChildrenDefinition> children)
    {
        // Convert to ChildrenBlock and process
        var childrenBlock = new ChildrenBlock(
            nestedChildren.CollectionName,
            nestedChildren.IdentifierExpression,
            nestedChildren.AutoMap,
            nestedChildren.ChildBlocks);

        ProcessChildrenBlock(childrenBlock, children);
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

    void ProcessChildEveryBlock(ChildEveryBlock everyBlock, FromEveryDefinition fromEvery)
    {
        // Cast IDictionary to Dictionary since we know it's a Dictionary instance
        var properties = (Dictionary<PropertyPath, string>)fromEvery.Properties;

        foreach (var operation in everyBlock.Mappings)
        {
            ProcessMappingOperation(operation, properties);
        }

        // Update automap if specified
        if (everyBlock.AutoMap != AutoMap.Inherit)
        {
            fromEvery.AutoMap = everyBlock.AutoMap;
        }
    }

    void ProcessRemoveWithDirective(RemoveWithDirective removeWith, Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        var eventType = EventType.Parse(removeWith.EventType.Name);
        var key = removeWith.Key != null ? ConvertExpression(removeWith.Key) : PropertyExpression.NotSet;

        removedWith[eventType] = new RemovedWithDefinition(key, ParentKey: null);
    }

    void ProcessRemoveWithJoinDirective(RemoveWithJoinDirective removeWithJoin, Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin)
    {
        var eventType = EventType.Parse(removeWithJoin.EventType.Name);
        var key = removeWithJoin.Key != null ? ConvertExpression(removeWithJoin.Key) : PropertyExpression.NotSet;

        removedWithJoin[eventType] = new RemovedWithJoinDefinition(key);
    }

    void ProcessJoinBlock(JoinBlock joinBlock, Dictionary<EventType, JoinDefinition> join)
    {
        // Each 'with' block creates a separate join definition
        foreach (var withBlock in joinBlock.WithBlocks)
        {
            var properties = new Dictionary<PropertyPath, string>();

            foreach (var operation in withBlock.Mappings)
            {
                ProcessMappingOperation(operation, properties);
            }

            var eventType = EventType.Parse(withBlock.EventType.Name);
            join[eventType] = new JoinDefinition(
                new PropertyPath(joinBlock.OnProperty),
                properties,
                PropertyExpression.NotSet);
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
            AutoMap = GetAutoMapValue(every.AutoMap)
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
            CausedByExpression causedBy => new PropertyExpression(causedBy.Property == null ? "$causedBy" : $"$causedBy({causedBy.Property})"),
            LiteralExpression literal => new PropertyExpression(FormatLiteralForStorage(literal.Value)),
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
            CausedByExpression causedBy => causedBy.Property == null ? "$causedBy" : $"$causedBy({causedBy.Property})",
            LiteralExpression literal => FormatLiteralForStorage(literal.Value),
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

    string FormatLiteralForStorage(object? value)
    {
        return value switch
        {
            null => string.Empty,  // Store null as empty string (expected by tests)
            string s => $"\"{s}\"",  // Store strings with quotes to distinguish from property names
            bool b => b.ToString(),  // Store as "True"/"False" (C# ToString() format expected by tests)
            _ => value.ToString() ?? string.Empty // Numbers as-is
        };
    }
}

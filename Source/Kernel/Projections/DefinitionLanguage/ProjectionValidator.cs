// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Validates projection definitions against read models and event type schemas.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionValidator"/> class.
/// </remarks>
/// <param name="readModelDefinitions">Available read model definitions.</param>
/// <param name="eventTypeSchemas">Available event type schemas.</param>
public class ProjectionValidator(
    IEnumerable<ReadModelDefinition> readModelDefinitions,
    IEnumerable<EventTypeSchema> eventTypeSchemas)
{
    readonly Dictionary<ReadModelIdentifier, ReadModelDefinition> _readModelLookup = readModelDefinitions.ToDictionary(_ => _.Identifier);
    readonly Dictionary<EventType, EventTypeSchema> _eventTypeLookup = eventTypeSchemas.ToDictionary(_ => _.Type);

    /// <summary>
    /// Validates a projection against the available read models and event type schemas.
    /// </summary>
    /// <param name="projection">The projection to validate.</param>
    /// <param name="errors">The compiler errors collection to add errors to.</param>
    /// <returns>The read model schema if validation succeeds, null otherwise.</returns>
    public JsonSchema? Validate(ProjectionNode projection, CompilerErrors errors)
    {
        var readModelIdentifier = new ReadModelIdentifier(projection.ReadModelType.Name);

        if (!_readModelLookup.TryGetValue(readModelIdentifier, out var readModelDefinition))
        {
            errors.Add($"Read model '{readModelIdentifier}' not found", projection.Line, projection.Column);
            return null;
        }

        var readModelSchema = readModelDefinition.GetSchemaForLatestGeneration();

        ValidateDirectives(projection.Directives, readModelSchema, errors);
        return readModelSchema;
    }

    void ValidateDirectives(IReadOnlyList<ProjectionDirective> directives, JsonSchema readModelSchema, CompilerErrors errors)
    {
        foreach (var directive in directives)
        {
            switch (directive)
            {
                case FromEventBlock fromEvent:
                    ValidateFromEventBlock(fromEvent, readModelSchema, errors);
                    break;
                case MultiFromEventBlock multiFromEvent:
                    foreach (var block in multiFromEvent.Blocks)
                    {
                        ValidateFromEventBlock(block, readModelSchema, errors);
                    }
                    break;
                case ChildrenBlock childrenBlock:
                    ValidateChildrenBlock(childrenBlock, readModelSchema, errors);
                    break;
                case JoinBlock:
                case EveryBlock:
                case KeyDirective:
                case AutoMapDirective:
                case NoAutoMapDirective:
                case RemoveWithDirective:
                case RemoveWithJoinDirective:
                    // These don't require validation at this stage
                    break;
                case CompositeKeyDirective compositeKey:
                    ValidateCompositeKeyDirective(compositeKey, readModelSchema, errors);
                    break;
            }
        }
    }

    void ValidateFromEventBlock(FromEventBlock fromEvent, JsonSchema readModelSchema, CompilerErrors errors)
    {
        var eventType = EventType.Parse(fromEvent.EventType.Name);

        if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
        {
            errors.Add($"Event type '{eventType}' not found", fromEvent.Line, fromEvent.Column);
            return;
        }

        // Validate composite key if present
        if (fromEvent.CompositeKey is not null)
        {
            ValidateCompositeKeyDirective(fromEvent.CompositeKey, readModelSchema, errors);
        }

        ValidateMappings(fromEvent.Mappings, readModelSchema, eventTypeSchema.Schema, errors);
    }

    void ValidateChildrenBlock(ChildrenBlock childrenBlock, JsonSchema readModelSchema, CompilerErrors errors)
    {
        var collectionPath = new PropertyPath(childrenBlock.CollectionName);

        if (!readModelSchema.Properties.TryGetValue(collectionPath.Path, out var collectionProperty))
        {
            errors.Add($"Read model property '{collectionPath.Path}' not found", childrenBlock.Line, childrenBlock.Column);
            return;
        }

        if (!collectionProperty.Type.HasFlag(JsonObjectType.Array))
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Expected array type", childrenBlock.Line, childrenBlock.Column);
            return;
        }

        var itemSchema = collectionProperty.Item?.ActualSchema;
        if (itemSchema is null)
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Array must have item schema", childrenBlock.Line, childrenBlock.Column);
            return;
        }

        foreach (var childBlock in childrenBlock.ChildBlocks)
        {
            ValidateChildBlock(childBlock, itemSchema, errors);
        }
    }

    void ValidateChildBlock(ChildBlock childBlock, JsonSchema itemSchema, CompilerErrors errors)
    {
        switch (childBlock)
        {
            case ChildOnEventBlock childOnEvent:
                ValidateChildOnEventBlock(childOnEvent, itemSchema, errors);
                break;
            case NestedChildrenBlock nestedChildren:
                ValidateNestedChildrenBlock(nestedChildren, itemSchema, errors);
                break;
        }
    }

    void ValidateNestedChildrenBlock(NestedChildrenBlock nestedChildrenBlock, JsonSchema itemSchema, CompilerErrors errors)
    {
        var collectionPath = new PropertyPath(nestedChildrenBlock.CollectionName);

        if (!itemSchema.Properties.TryGetValue(collectionPath.Path, out var collectionProperty))
        {
            errors.Add($"Read model property '{collectionPath.Path}' not found", nestedChildrenBlock.Line, nestedChildrenBlock.Column);
            return;
        }

        if (!collectionProperty.Type.HasFlag(JsonObjectType.Array))
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Expected array type", nestedChildrenBlock.Line, nestedChildrenBlock.Column);
            return;
        }

        var nestedItemSchema = collectionProperty.Item?.ActualSchema;
        if (nestedItemSchema is null)
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Array must have item schema", nestedChildrenBlock.Line, nestedChildrenBlock.Column);
            return;
        }

        foreach (var childBlock in nestedChildrenBlock.ChildBlocks)
        {
            ValidateChildBlock(childBlock, nestedItemSchema, errors);
        }
    }

    void ValidateChildOnEventBlock(ChildOnEventBlock childOnEvent, JsonSchema itemSchema, CompilerErrors errors)
    {
        var eventType = EventType.Parse(childOnEvent.EventType.Name);

        if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
        {
            errors.Add($"Event type '{eventType}' not found", childOnEvent.Line, childOnEvent.Column);
            return;
        }

        ValidateMappings(childOnEvent.Mappings, itemSchema, eventTypeSchema.Schema, errors);
    }

    void ValidateMappings(IReadOnlyList<MappingOperation> mappings, JsonSchema targetSchema, JsonSchema eventSchema, CompilerErrors errors)
    {
        foreach (var mapping in mappings)
        {
            switch (mapping)
            {
                case AssignmentOperation assignment:
                    ValidateAssignmentOperation(assignment, targetSchema, eventSchema, errors);
                    break;
                case AddOperation:
                case IncrementOperation:
                case DecrementOperation:
                case SubtractOperation:
                case CountOperation:
                    // These operations are validated separately if needed
                    break;
            }
        }
    }

    void ValidateAssignmentOperation(AssignmentOperation assignment, JsonSchema targetSchema, JsonSchema eventSchema, CompilerErrors errors)
    {
        var targetPath = assignment.PropertyName;

        if (!TryResolveProperty(targetSchema, targetPath, out var targetProperty))
        {
            errors.Add($"Read model property '{targetPath}' not found", assignment.Line, assignment.Column);
            return;
        }

        // Validate the source expression and type compatibility
        if (assignment.Value is EventDataExpression eventDataExpression)
        {
            var sourcePath = eventDataExpression.Path;

            if (!TryResolveProperty(eventSchema, sourcePath, out var sourceProperty))
            {
                errors.Add($"Event property '{sourcePath}' not found", assignment.Line, assignment.Column);
                return;
            }

            // Validate that types are compatible
            if (!AreTypesCompatible(targetProperty.Type, sourceProperty.Type))
            {
                errors.Add($"Type mismatch: Cannot assign '{sourcePath}' of type '{sourceProperty.Type}' to '{targetPath}' of type '{targetProperty.Type}'", assignment.Line, assignment.Column);
            }
        }
    }

    bool TryResolveProperty(JsonSchema schema, string path, out JsonSchemaProperty property)
    {
        property = null!;
        var parts = path.Split('.');
        var currentSchema = schema;

        foreach (var part in parts)
        {
            if (!currentSchema.Properties.TryGetValue(part, out var prop))
            {
                return false;
            }

            property = prop;

            // If this isn't the last part, navigate to the nested schema
            if (part != parts[^1])
            {
                if (prop.ActualSchema.Type == JsonObjectType.Object)
                {
                    currentSchema = prop.ActualSchema;
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    void ValidateCompositeKeyDirective(CompositeKeyDirective compositeKey, JsonSchema readModelSchema, CompilerErrors errors)
    {
        var typeName = compositeKey.TypeName.Name;

        // Check if the composite key type exists in the read model schema
        JsonSchema? keySchema = null;

        // First, check in the definitions (most likely place for complex types)
        if (readModelSchema.Definitions.TryGetValue(typeName, out var definedType))
        {
            keySchema = definedType;
        }
        // If not in definitions, check if it's a property in the read model (camelCase)
        else
        {
            var camelCaseTypeName = LowercaseFirstLetter(typeName);
            if (readModelSchema.Properties.TryGetValue(camelCaseTypeName, out var keyProperty))
            {
                keySchema = keyProperty.ActualSchema;
            }
        }

        if (keySchema is null)
        {
            errors.Add($"Composite key type '{typeName}' not found in read model schema", compositeKey.Line, compositeKey.Column);
            return;
        }

        // Validate that it's a complex type (object)
        // Note: Nullable types might have JsonObjectType.Null flag set along with Object
        // Also, some schemas might not have Type set but have Properties, which indicates it's an object
        var isObject = keySchema.Type.HasFlag(JsonObjectType.Object) ||
                      (keySchema.Properties.Count > 0 && keySchema.Type == JsonObjectType.None);

        if (!isObject)
        {
            errors.Add($"Composite key type '{typeName}' must be a complex type (object) in the read model schema", compositeKey.Line, compositeKey.Column);
            return;
        }

        // Validate each key part
        foreach (var part in compositeKey.Parts)
        {
            // Validate that the property exists in the composite key type schema
            if (!keySchema.Properties.ContainsKey(part.PropertyName))
            {
                errors.Add($"Property '{part.PropertyName}' not found in composite key type '{typeName}'", part.Line, part.Column);
                continue;
            }

            // Composite keys only support simple assignment expressions (key = value)
            // Validate expression type is supported
            switch (part.Expression)
            {
                case EventDataExpression:
                case EventContextExpression:
                case EventSourceIdExpression:
                case CausedByExpression:
                case LiteralExpression:
                    // These are valid for composite keys
                    break;
                case TemplateExpression:
                    errors.Add("Template expressions are not supported in composite keys. Use simple expressions only.", part.Line, part.Column);
                    continue;
                default:
                    errors.Add($"Expression type '{part.Expression.GetType().Name}' is not supported in composite keys", part.Line, part.Column);
                    continue;
            }

            // Note: Type compatibility validation between expression and property type could be added here
            // but would require knowing the event schema context, which isn't available at this level
        }
    }

    bool AreTypesCompatible(JsonObjectType targetType, JsonObjectType sourceType)
    {
        // Exact match
        if (targetType == sourceType)
        {
            return true;
        }

        // If target is nullable (has Null flag), check if source type (without null) is compatible
        if (targetType.HasFlag(JsonObjectType.Null))
        {
            var targetWithoutNull = targetType & ~JsonObjectType.Null;
            if (targetWithoutNull == sourceType)
            {
                return true;
            }

            // Check numeric compatibility for nullable targets
            var numericTypes = new[] { JsonObjectType.Integer, JsonObjectType.Number };
            if (numericTypes.Contains(targetWithoutNull) && numericTypes.Contains(sourceType))
            {
                return true;
            }
        }

        // Allow numeric conversions
        var numericTypesForNonNullable = new[] { JsonObjectType.Integer, JsonObjectType.Number };
        return numericTypesForNonNullable.Contains(targetType) && numericTypesForNonNullable.Contains(sourceType);
    }

    static string LowercaseFirstLetter(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}

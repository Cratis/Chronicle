// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Validates Screenplay projection syntax against read models and event type schemas.
/// </summary>
/// <param name="readModelDefinitions">Available read model definitions.</param>
/// <param name="eventTypeSchemas">Available event type schemas.</param>
public class ProjectionValidator(
    IEnumerable<ReadModelDefinition> readModelDefinitions,
    IEnumerable<EventTypeSchema> eventTypeSchemas)
{
    readonly Dictionary<ReadModelIdentifier, ReadModelDefinition> _readModelLookup = readModelDefinitions.DistinctBy(_ => _.Identifier).ToDictionary(_ => _.Identifier);
    readonly Dictionary<EventType, EventTypeSchema> _eventTypeLookup = eventTypeSchemas.DistinctBy(_ => _.Type).ToDictionary(_ => _.Type);

    /// <summary>
    /// Validates a projection against the available read models and event type schemas.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> to validate.</param>
    /// <param name="errors">The compiler errors collection to add errors to.</param>
    /// <returns>The read model schema if validation succeeds, null otherwise.</returns>
    public JsonSchema? Validate(ProjectionSyntax projection, CompilerErrors errors)
    {
        if (projection.ReadModel is null)
        {
            return ValidateAndInferSchema(projection, errors);
        }

        var readModelIdentifier = new ReadModelIdentifier(projection.ReadModel);

        if (!_readModelLookup.TryGetValue(readModelIdentifier, out var readModelDefinition))
        {
            errors.Add($"Read model '{readModelIdentifier}' not found", projection.Location.Line, projection.Location.Column);
            return null;
        }

        var readModelSchema = readModelDefinition.GetSchemaForLatestGeneration();

        ValidateDuplicateEvents(projection.Blocks, errors);
        ValidateBlocks(projection, readModelSchema, errors);
        return readModelSchema;
    }

    /// <summary>
    /// Validates a projection without an explicit read model and infers the schema from event types.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> to validate.</param>
    /// <param name="errors">The compiler errors collection to add errors to.</param>
    /// <returns>The inferred read model schema if validation succeeds, null otherwise.</returns>
    public JsonSchema? ValidateAndInferSchema(ProjectionSyntax projection, CompilerErrors errors)
    {
        ValidateDuplicateEvents(projection.Blocks, errors);

        var aggregatedEventProperties = new Dictionary<string, (JsonObjectType Type, string? Format)>(StringComparer.Ordinal);

        CollectEventProperties(projection.Blocks, aggregatedEventProperties, errors);

        if (errors.HasErrors)
        {
            return null;
        }

        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, (type, format)) in aggregatedEventProperties)
        {
            schema.Properties[name] = new JsonSchemaProperty { Type = type, Format = format };
        }

        return schema;
    }

    static string LowercaseFirstLetter(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    void ValidateDuplicateEvents(IEnumerable<ProjectionBlockSyntax> blocks, CompilerErrors errors)
    {
        var seenEvents = new HashSet<string>(StringComparer.Ordinal);

        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var eventSpec in from.Events)
                    {
                        CheckDuplicateEvent(eventSpec.Event, eventSpec.Location, seenEvents, errors);
                    }
                    break;
                case ChildrenSyntax children:
                    ValidateDuplicateEvents(children.Blocks, errors);
                    break;
                case JoinSyntax join:
                    ValidateDuplicateEventsInJoin(join, errors);
                    break;
                case RemoveWithSyntax removeWith:
                    CheckDuplicateEvent(removeWith.Event, removeWith.Location, seenEvents, errors);
                    break;
                case RemoveViaJoinSyntax removeViaJoin:
                    CheckDuplicateEvent(removeViaJoin.Event, removeViaJoin.Location, seenEvents, errors);
                    break;
            }
        }
    }

    void ValidateDuplicateEventsInJoin(JoinSyntax join, CompilerErrors errors)
    {
        var seenEvents = new HashSet<string>(StringComparer.Ordinal);

        foreach (var joinEvent in join.Events)
        {
            CheckDuplicateEvent(joinEvent.Event, joinEvent.Location, seenEvents, errors);
        }
    }

    void CheckDuplicateEvent(string eventName, SourceLocation location, HashSet<string> seenEvents, CompilerErrors errors)
    {
        if (!seenEvents.Add(eventName))
        {
            errors.Add($"Duplicate event type '{eventName}' - event types can only be used once at each level", location.Line, location.Column);
        }
    }

    void ValidateBlocks(ProjectionSyntax projection, JsonSchema readModelSchema, CompilerErrors errors)
    {
        if (projection.Key is CompositeKeySyntax projectionCompositeKey)
        {
            ValidateCompositeKey(projectionCompositeKey, readModelSchema, errors);
        }

        foreach (var block in projection.Blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    ValidateFrom(from, readModelSchema, errors);
                    break;
                case ChildrenSyntax children:
                    ValidateChildren(children, readModelSchema, errors);
                    break;
                case JoinSyntax join:
                    ValidateJoin(join, readModelSchema, errors);
                    break;
                case RemoveWithSyntax removeWith:
                    ValidateEventTypeExists(removeWith.Event, removeWith.Location, errors);
                    break;
                case RemoveViaJoinSyntax removeViaJoin:
                    ValidateEventTypeExists(removeViaJoin.Event, removeViaJoin.Location, errors);
                    break;
            }
        }
    }

    void ValidateEventTypeExists(string eventName, SourceLocation location, CompilerErrors errors)
    {
        var eventType = EventType.Parse(eventName);

        if (!_eventTypeLookup.ContainsKey(eventType))
        {
            errors.Add($"Event type '{eventType.Id}' not found", location.Line, location.Column);
        }
    }

    void ValidateFrom(FromSyntax from, JsonSchema readModelSchema, CompilerErrors errors)
    {
        if (from.Key is CompositeKeySyntax compositeKey)
        {
            ValidateCompositeKey(compositeKey, readModelSchema, errors);
        }

        foreach (var eventSpec in from.Events)
        {
            var eventType = EventType.Parse(eventSpec.Event);

            if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
            {
                errors.Add($"Event type '{eventType.Id}' not found", eventSpec.Location.Line, eventSpec.Location.Column);
                continue;
            }

            ValidateMappings(from.Mappings, readModelSchema, eventTypeSchema.Schema, errors);
        }
    }

    void ValidateJoin(JoinSyntax join, JsonSchema readModelSchema, CompilerErrors errors)
    {
        foreach (var joinEvent in join.Events)
        {
            var eventType = EventType.Parse(joinEvent.Event);

            if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
            {
                errors.Add($"Event type '{eventType.Id}' not found", joinEvent.Location.Line, joinEvent.Location.Column);
                continue;
            }

            ValidateMappings(joinEvent.Mappings, readModelSchema, eventTypeSchema.Schema, errors);
        }
    }

    void ValidateChildren(ChildrenSyntax children, JsonSchema parentSchema, CompilerErrors errors)
    {
        var collectionPath = new PropertyPath(children.Property);

        if (!parentSchema.Properties.TryGetValue(collectionPath.Path, out var collectionProperty))
        {
            errors.Add($"Read model property '{collectionPath.Path}' not found", children.Location.Line, children.Location.Column);
            return;
        }

        if (!collectionProperty.Type.HasFlag(JsonObjectType.Array))
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Expected array type", children.Location.Line, children.Location.Column);
            return;
        }

        var itemSchema = collectionProperty.Item?.ActualSchema;
        if (itemSchema is null)
        {
            errors.Add($"Read model property '{collectionPath.Path}' is invalid: Array must have item schema", children.Location.Line, children.Location.Column);
            return;
        }

        foreach (var block in children.Blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    ValidateChildFrom(from, itemSchema, errors);
                    break;
                case ChildrenSyntax nestedChildren:
                    ValidateChildren(nestedChildren, itemSchema, errors);
                    break;
            }
        }
    }

    void ValidateChildFrom(FromSyntax from, JsonSchema itemSchema, CompilerErrors errors)
    {
        foreach (var eventSpec in from.Events)
        {
            var eventType = EventType.Parse(eventSpec.Event);

            if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
            {
                errors.Add($"Event type '{eventType.Id}' not found", eventSpec.Location.Line, eventSpec.Location.Column);
                continue;
            }

            ValidateMappings(from.Mappings, itemSchema, eventTypeSchema.Schema, errors);
        }
    }

    void ValidateMappings(IEnumerable<MappingSyntax> mappings, JsonSchema targetSchema, JsonSchema eventSchema, CompilerErrors errors)
    {
        foreach (var mapping in mappings)
        {
            switch (mapping)
            {
                case SetMappingSyntax set:
                    ValidateSetMapping(set, targetSchema, eventSchema, errors);
                    break;
                case AddMappingSyntax add:
                    ValidatePropertyExists(add.Property, targetSchema, errors, add);
                    ValidateEventPropertyExists(add.Value, eventSchema, errors);
                    break;
                case SubtractMappingSyntax subtract:
                    ValidatePropertyExists(subtract.Property, targetSchema, errors, subtract);
                    ValidateEventPropertyExists(subtract.Value, eventSchema, errors);
                    break;
                case CountMappingSyntax:
                case IncrementMappingSyntax:
                case DecrementMappingSyntax:
                    ValidatePropertyExists(mapping.Property, targetSchema, errors, mapping);
                    break;
            }
        }
    }

    void ValidatePropertyExists(string propertyName, JsonSchema targetSchema, CompilerErrors errors, MappingSyntax mapping)
    {
        if (!TryResolveProperty(targetSchema, propertyName, out _))
        {
            errors.Add($"Read model property '{propertyName}' not found", mapping.Location.Line, mapping.Location.Column);
        }
    }

    void ValidateEventPropertyExists(ExpressionSyntax value, JsonSchema eventSchema, CompilerErrors errors)
    {
        if (value is PathExpressionSyntax path && !TryResolveProperty(eventSchema, path.Path, out _))
        {
            errors.Add($"Event property '{path.Path}' not found", value.Location.Line, value.Location.Column);
        }
    }

    void ValidateSetMapping(SetMappingSyntax set, JsonSchema targetSchema, JsonSchema eventSchema, CompilerErrors errors)
    {
        var targetPath = set.Property;

        if (!TryResolveProperty(targetSchema, targetPath, out var targetProperty))
        {
            errors.Add($"Read model property '{targetPath}' not found", set.Location.Line, set.Location.Column);
            return;
        }

        // Validate the source expression and type compatibility
        if (set.Source is PathExpressionSyntax path)
        {
            var sourcePath = path.Path;

            if (!TryResolveProperty(eventSchema, sourcePath, out var sourceProperty))
            {
                errors.Add($"Event property '{sourcePath}' not found", set.Location.Line, set.Location.Column);
                return;
            }

            if (!AreTypesCompatible(targetProperty.Type, sourceProperty.Type))
            {
                errors.Add($"Type mismatch: Cannot assign '{sourcePath}' of type '{sourceProperty.Type}' to '{targetPath}' of type '{targetProperty.Type}'", set.Location.Line, set.Location.Column);
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

    void ValidateCompositeKey(CompositeKeySyntax compositeKey, JsonSchema readModelSchema, CompilerErrors errors)
    {
        var typeName = compositeKey.Type;

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
            else
            {
                // Fallback: search for a property whose schema title matches the type name.
                // This handles cases where the property name differs from the type name
                // (e.g., property "key" of type "KeywordKey").
                var matchByTitle = readModelSchema.Properties
                    .FirstOrDefault(p => string.Equals(p.Value.ActualSchema.Title, typeName, StringComparison.Ordinal));
                if (matchByTitle.Value is not null)
                {
                    keySchema = matchByTitle.Value.ActualSchema;
                }
            }
        }

        if (keySchema is null)
        {
            errors.Add($"Composite key type '{typeName}' not found in read model schema", compositeKey.Location.Line, compositeKey.Location.Column);
            return;
        }

        // Validate that it's a complex type (object)
        // Note: Nullable types might have JsonObjectType.Null flag set along with Object
        // Also, some schemas might not have Type set but have Properties, which indicates it's an object
        var isObject = keySchema.Type.HasFlag(JsonObjectType.Object) ||
                      (keySchema.Properties.Count > 0 && keySchema.Type == JsonObjectType.None);

        if (!isObject)
        {
            errors.Add($"Composite key type '{typeName}' must be a complex type (object) in the read model schema", compositeKey.Location.Line, compositeKey.Location.Column);
            return;
        }

        // Validate each key part
        foreach (var part in compositeKey.Parts)
        {
            // Validate that the property exists in the composite key type schema
            if (!keySchema.Properties.ContainsKey(part.Property))
            {
                errors.Add($"Property '{part.Property}' not found in composite key type '{typeName}'", part.Location.Line, part.Location.Column);
                continue;
            }

            // Composite keys only support simple assignment expressions (key = value)
            // Validate expression type is supported
            switch (part.Expression)
            {
                case PathExpressionSyntax:
                case EventContextExpressionSyntax:
                case EventSourceIdExpressionSyntax:
                case CausedByExpressionSyntax:
                case LiteralExpressionSyntax:
                    // These are valid for composite keys
                    break;
                case TemplateExpressionSyntax:
                    errors.Add("Template expressions are not supported in composite keys. Use simple expressions only.", part.Location.Line, part.Location.Column);
                    continue;
                default:
                    errors.Add($"Expression type '{part.Expression.GetType().Name}' is not supported in composite keys", part.Location.Line, part.Location.Column);
                    continue;
            }
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

    void CollectEventProperties(
        IEnumerable<ProjectionBlockSyntax> blocks,
        Dictionary<string, (JsonObjectType Type, string? Format)> aggregatedEventProperties,
        CompilerErrors errors)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var eventSpec in from.Events)
                    {
                        CollectEventTypeProperties(eventSpec, aggregatedEventProperties, errors);
                    }
                    break;
                case RemoveWithSyntax removeWith:
                    ValidateEventTypeExists(removeWith.Event, removeWith.Location, errors);
                    break;
                case RemoveViaJoinSyntax removeViaJoin:
                    ValidateEventTypeExists(removeViaJoin.Event, removeViaJoin.Location, errors);
                    break;
            }
        }
    }

    void CollectEventTypeProperties(
        EventSpecSyntax eventSpec,
        Dictionary<string, (JsonObjectType Type, string? Format)> aggregatedEventProperties,
        CompilerErrors errors)
    {
        var eventType = EventType.Parse(eventSpec.Event);

        if (!_eventTypeLookup.TryGetValue(eventType, out var eventTypeSchema))
        {
            errors.Add($"Event type '{eventType.Id}' not found", eventSpec.Location.Line, eventSpec.Location.Column);
            return;
        }

        foreach (var (name, prop) in eventTypeSchema.Schema.Properties)
        {
            var propType = prop.ActualTypeSchema?.Type ?? prop.Type;
            var propFormat = prop.Format;

            if (aggregatedEventProperties.TryGetValue(name, out var existing))
            {
                // Check type compatibility between events that share property names.
                // Two properties are incompatible when their base types differ; format differences
                // (e.g. date-time vs plain string) are also considered incompatible.
                if (!AreTypesCompatible(existing.Type, propType) ||
                    existing.Format != propFormat)
                {
                    errors.Add(
                        $"Property '{name}' has incompatible types across events: '{existing.Type}' (format: '{existing.Format}') vs '{propType}' (format: '{propFormat}')",
                        eventSpec.Location.Line,
                        eventSpec.Location.Column);
                }
            }
            else
            {
                aggregatedEventProperties[name] = (propType, propFormat);
            }
        }
    }
}

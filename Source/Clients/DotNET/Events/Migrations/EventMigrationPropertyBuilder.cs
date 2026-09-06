// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationPropertyBuilder"/>.
/// </summary>
public class EventMigrationPropertyBuilder : IEventMigrationPropertyBuilder
{
    const string SplitExpression = "$split";
    const string CombineExpression = "$combine";
    const string RenameExpression = "$rename";
    const string DefaultValueExpression = "$defaultValue";
    const string MapValuesExpression = "$mapValues";

    readonly Dictionary<PropertyExpression, JsonNode> _properties = [];

    /// <summary>
    /// Gets the configured properties.
    /// </summary>
    public IReadOnlyDictionary<PropertyExpression, JsonNode> Properties => _properties;

    /// <inheritdoc/>
    public void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [SplitExpression] = new JsonObject
            {
                ["source"] = (string)sourceProperty,
                ["separator"] = (string)separator,
                ["part"] = (int)part
            }
        };
    }

    /// <inheritdoc/>
    public void Combine(PropertyName targetProperty, PropertySeparator separator, params PropertyName[] sourceProperties)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [CombineExpression] = new JsonObject
            {
                ["sources"] = new JsonArray(sourceProperties.Select(p => JsonValue.Create((string)p)).ToArray()),
                ["separator"] = (string)separator
            }
        };
    }

    /// <inheritdoc/>
    public void RenamedFrom(PropertyName targetProperty, PropertyName oldName)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [RenameExpression] = (string)oldName
        };
    }

    /// <inheritdoc/>
    public void DefaultValue(PropertyName targetProperty, object value)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
    }

    /// <inheritdoc/>
    public void MapValues(PropertyName targetProperty, PropertyName sourceProperty, IEnumerable<ValueMapping> mappings)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [MapValuesExpression] = new JsonObject
            {
                ["source"] = (string)sourceProperty,
                ["mappings"] = new JsonArray([.. mappings.Select(ToMappingNode)])
            }
        };
    }

    static JsonNode ToMappingNode(ValueMapping mapping) => new JsonObject
    {
        ["from"] = ToJsonNode(mapping.From),
        ["to"] = ToJsonNode(mapping.To)
    };

    /// <summary>
    /// Renders a mapped value the way the value will appear in an event's payload.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>The value as a <see cref="JsonNode"/>.</returns>
    /// <remarks>
    /// An enum is rendered as its underlying numeric value, which is what a payload carries - rendering it by name
    /// would produce a map that never matches anything.
    /// </remarks>
    static JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        Enum enumValue => JsonSerializer.SerializeToNode(
            Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture)),
        _ => JsonSerializer.SerializeToNode(value, value.GetType())
    };
}

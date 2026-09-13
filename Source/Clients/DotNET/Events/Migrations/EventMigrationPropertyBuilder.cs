// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationPropertyBuilder"/>.
/// </summary>
/// <param name="namingPolicy">Optional <see cref="INamingPolicy"/> used to render property names the way they appear in an event's payload. Defaults to <see cref="DefaultNamingPolicy"/>.</param>
public class EventMigrationPropertyBuilder(INamingPolicy? namingPolicy = null) : IEventMigrationPropertyBuilder
{
    const string SplitExpression = "$split";
    const string CombineExpression = "$combine";
    const string RenameExpression = "$rename";
    const string DefaultValueExpression = "$defaultValue";
    const string MapValuesExpression = "$mapValues";

    readonly Dictionary<PropertyExpression, JsonNode> _properties = [];
    readonly INamingPolicy _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();

    /// <summary>
    /// Gets the configured properties.
    /// </summary>
    public IReadOnlyDictionary<PropertyExpression, JsonNode> Properties => _properties;

    /// <inheritdoc/>
    public void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part)
    {
        _properties[(PropertyExpression)Render(targetProperty)] = new JsonObject
        {
            [SplitExpression] = new JsonObject
            {
                ["source"] = Render(sourceProperty),
                ["separator"] = (string)separator,
                ["part"] = (int)part
            }
        };
    }

    /// <inheritdoc/>
    public void Combine(PropertyName targetProperty, PropertySeparator separator, params PropertyName[] sourceProperties)
    {
        _properties[(PropertyExpression)Render(targetProperty)] = new JsonObject
        {
            [CombineExpression] = new JsonObject
            {
                ["sources"] = new JsonArray(sourceProperties.Select(p => JsonValue.Create(Render(p))).ToArray()),
                ["separator"] = (string)separator
            }
        };
    }

    /// <inheritdoc/>
    public void RenamedFrom(PropertyName targetProperty, PropertyName oldName)
    {
        _properties[(PropertyExpression)Render(targetProperty)] = new JsonObject
        {
            [RenameExpression] = Render(oldName)
        };
    }

    /// <inheritdoc/>
    public void DefaultValue(PropertyName targetProperty, object value)
    {
        _properties[(PropertyExpression)Render(targetProperty)] = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
    }

    /// <inheritdoc/>
    public void MapValues(PropertyName targetProperty, PropertyName sourceProperty, IEnumerable<ValueMapping> mappings)
    {
        _properties[(PropertyExpression)Render(targetProperty)] = new JsonObject
        {
            [MapValuesExpression] = new JsonObject
            {
                ["source"] = Render(sourceProperty),
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

    /// <summary>
    /// Renders a property name the way it appears in an event's payload.
    /// </summary>
    /// <param name="property">The <see cref="PropertyName"/> to render.</param>
    /// <returns>The rendered name.</returns>
    /// <remarks>
    /// A migration map is matched against the serialized payload, so its names have to go through the same
    /// naming policy the payload was written with. Without this the map silently matches nothing.
    /// </remarks>
    string Render(PropertyName property) => _namingPolicy.GetPropertyName(new PropertyPath((string)property));
}

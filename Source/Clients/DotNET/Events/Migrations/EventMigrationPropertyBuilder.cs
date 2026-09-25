// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationPropertyBuilder"/>.
/// </summary>
/// <param name="namingPolicy">The naming policy for typed accessors. Raw property paths are preserved verbatim.</param>
public class EventMigrationPropertyBuilder(INamingPolicy? namingPolicy) : IEventMigrationPropertyBuilder, IResolveMigrationPropertyNames
{
    const string SplitExpression = "$split";
    const string CombineExpression = "$combine";
    const string RenameExpression = "$rename";
    const string DefaultValueExpression = "$defaultValue";
    const string MapValuesExpression = "$mapValues";

    readonly Dictionary<PropertyExpression, JsonNode> _properties = [];

    /// <summary>
    /// Initializes a new instance of <see cref="EventMigrationPropertyBuilder"/> using the default naming policy.
    /// </summary>
    public EventMigrationPropertyBuilder() : this(null)
    {
    }

    /// <summary>
    /// Gets the configured properties.
    /// </summary>
    public IReadOnlyDictionary<PropertyExpression, JsonNode> Properties => _properties;

    /// <inheritdoc/>
    public void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part)
    {
        _properties[(PropertyExpression)targetProperty.Value] = new JsonObject
        {
            [SplitExpression] = new JsonObject
            {
                ["source"] = sourceProperty.Value,
                ["separator"] = (string)separator,
                ["part"] = (int)part
            }
        };
    }

    /// <inheritdoc/>
    public void Combine(PropertyName targetProperty, PropertySeparator separator, params PropertyName[] sourceProperties)
    {
        _properties[(PropertyExpression)targetProperty.Value] = new JsonObject
        {
            [CombineExpression] = new JsonObject
            {
                ["sources"] = new JsonArray(sourceProperties.Select(p => JsonValue.Create(p.Value)).ToArray()),
                ["separator"] = (string)separator
            }
        };
    }

    /// <inheritdoc/>
    public void RenamedFrom(PropertyName targetProperty, PropertyName oldName)
    {
        _properties[(PropertyExpression)targetProperty.Value] = new JsonObject
        {
            [RenameExpression] = oldName.Value
        };
    }

    /// <inheritdoc/>
    public void DefaultValue(PropertyName targetProperty, object value)
    {
        _properties[(PropertyExpression)targetProperty.Value] = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
    }

    /// <inheritdoc/>
    public void MapValues(PropertyName targetProperty, PropertyName sourceProperty, IEnumerable<ValueMapping> mappings)
    {
        _properties[(PropertyExpression)targetProperty.Value] = new JsonObject
        {
            [MapValuesExpression] = new JsonObject
            {
                ["source"] = sourceProperty.Value,
                ["mappings"] = new JsonArray([.. mappings.Select(ToMappingNode)])
            }
        };
    }

    /// <inheritdoc/>
    PropertyName IResolveMigrationPropertyNames.ResolvePropertyName(LambdaExpression expression) =>
        MigrationPropertyNames.Resolve(expression, namingPolicy?.JsonPropertyNamingPolicy);

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

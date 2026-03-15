// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    readonly Dictionary<PropertyExpression, JsonNode> _properties = [];

    /// <summary>
    /// Gets the configured properties.
    /// </summary>
    public IReadOnlyDictionary<PropertyExpression, JsonNode> Properties => _properties;

    /// <inheritdoc/>
    public PropertyExpression Split(PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part)
    {
        var expression = new JsonObject
        {
            [SplitExpression] = new JsonObject
            {
                ["source"] = (string)sourceProperty,
                ["separator"] = (string)separator,
                ["part"] = (int)part
            }
        };
        return AddExpression(expression);
    }

    /// <inheritdoc/>
    public PropertyExpression Combine(params PropertyName[] properties)
    {
        var expression = new JsonObject
        {
            [CombineExpression] = new JsonArray(properties.Select(p => JsonValue.Create((string)p)).ToArray())
        };
        return AddExpression(expression);
    }

    /// <inheritdoc/>
    public PropertyExpression RenamedFrom(PropertyName oldName)
    {
        var expression = new JsonObject
        {
            [RenameExpression] = (string)oldName
        };
        return AddExpression(expression);
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
    public PropertyExpression DefaultValue(object value)
    {
        var expression = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
        return AddExpression(expression);
    }

    PropertyExpression AddExpression(JsonObject expression)
    {
        PropertyExpression key = $"__expr_{_properties.Count}";
        _properties[key] = expression;
        return key;
    }
}

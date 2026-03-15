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
    public void Split(PropertyName targetProperty, PropertyName sourceProperty, PropertySeparator separator, SplitPartIndex part)
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
        _properties[(PropertyExpression)(string)targetProperty] = expression;
    }

    /// <inheritdoc/>
    public void Combine(PropertyName targetProperty, params PropertyName[] sourceProperties)
    {
        var expression = new JsonObject
        {
            [CombineExpression] = new JsonArray(sourceProperties.Select(p => JsonValue.Create((string)p)).ToArray())
        };
        _properties[(PropertyExpression)(string)targetProperty] = expression;
    }

    /// <inheritdoc/>
    public void RenamedFrom(PropertyName targetProperty, PropertyName oldName)
    {
        var expression = new JsonObject
        {
            [RenameExpression] = (string)oldName
        };
        _properties[(PropertyExpression)(string)targetProperty] = expression;
    }

    /// <inheritdoc/>
    public void DefaultValue(PropertyName targetProperty, object value)
    {
        _properties[(PropertyExpression)(string)targetProperty] = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
    }
}

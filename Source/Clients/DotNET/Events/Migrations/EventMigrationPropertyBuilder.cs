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

    readonly Dictionary<string, JsonNode> _properties = [];

    /// <summary>
    /// Gets the configured properties.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode> Properties => _properties;

    /// <inheritdoc/>
    public string Split(string sourceProperty, string separator, int part)
    {
        var expression = new JsonObject
        {
            [SplitExpression] = new JsonObject
            {
                ["source"] = sourceProperty,
                ["separator"] = separator,
                ["part"] = part
            }
        };
        return AddExpression(expression);
    }

    /// <inheritdoc/>
    public string Combine(params string[] properties)
    {
        var expression = new JsonObject
        {
            [CombineExpression] = new JsonArray(properties.Select(p => JsonValue.Create(p)).ToArray())
        };
        return AddExpression(expression);
    }

    /// <inheritdoc/>
    public string RenamedFrom(string oldName)
    {
        var expression = new JsonObject
        {
            [RenameExpression] = oldName
        };
        return AddExpression(expression);
    }

    /// <inheritdoc/>
    public string DefaultValue(object value)
    {
        var expression = new JsonObject
        {
            [DefaultValueExpression] = JsonValue.Create(value)
        };
        return AddExpression(expression);
    }

    string AddExpression(JsonObject expression)
    {
        var key = $"__expr_{_properties.Count}";
        _properties[key] = expression;
        return key;
    }
}

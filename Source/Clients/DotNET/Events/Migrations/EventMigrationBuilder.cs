// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationBuilder"/>.
/// </summary>
public class EventMigrationBuilder : IEventMigrationBuilder
{
    readonly List<EventMigrationPropertyBuilder> _propertyBuilders = [];

    /// <summary>
    /// Gets all the property builders.
    /// </summary>
    public IReadOnlyList<EventMigrationPropertyBuilder> PropertyBuilders => _propertyBuilders;

    /// <inheritdoc/>
    public void Properties(Action<IEventMigrationPropertyBuilder> properties)
    {
        var builder = new EventMigrationPropertyBuilder();
        properties(builder);
        _propertyBuilders.Add(builder);
    }

    /// <summary>
    /// Convert the builder to a JSON object.
    /// </summary>
    /// <returns>The <see cref="JsonObject"/> representation.</returns>
    public JsonObject ToJson()
    {
        var result = new JsonObject();
        foreach (var builder in _propertyBuilders)
        {
            foreach (var property in builder.Properties)
            {
                result[property.Key] = property.Value;
            }
        }
        return result;
    }
}

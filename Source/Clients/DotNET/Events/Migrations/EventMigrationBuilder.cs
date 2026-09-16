// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents an implementation of <see cref="IEventMigrationBuilder"/>.
/// </summary>
/// <param name="namingPolicy">The naming policy for typed property accessors. Raw JSON paths are never renamed. Defaults to <see cref="DefaultNamingPolicy"/>.</param>
public class EventMigrationBuilder(INamingPolicy? namingPolicy) : IEventMigrationBuilder
{
    readonly List<EventMigrationPropertyBuilder> _propertyBuilders = [];
    readonly INamingPolicy _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();

    /// <summary>
    /// Initializes a new instance of <see cref="EventMigrationBuilder"/> using the default naming policy.
    /// </summary>
    public EventMigrationBuilder() : this(null)
    {
    }

    /// <summary>
    /// Gets all the property builders.
    /// </summary>
    public IReadOnlyList<EventMigrationPropertyBuilder> PropertyBuilders => _propertyBuilders;

    /// <inheritdoc/>
    public void Properties(Action<IEventMigrationPropertyBuilder> properties)
    {
        var builder = new EventMigrationPropertyBuilder(_namingPolicy);
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
                result[(string)property.Key] = property.Value;
            }
        }
        return result;
    }
}

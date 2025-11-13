// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations;

/// <summary>
/// Defines a system for migrating events between generations.
/// </summary>
public interface IEventTypeMigrations
{
    /// <summary>
    /// Migrate an event to all its generations (both upcast and downcast).
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> being migrated.</param>
    /// <param name="content">The event content as <see cref="JsonObject"/>.</param>
    /// <returns>A dictionary mapping each generation to its corresponding content.</returns>
    Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(EventType eventType, JsonObject content);
}

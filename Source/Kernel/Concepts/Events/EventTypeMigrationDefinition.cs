// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the definition of a migration between two event type generations.
/// </summary>
/// <param name="FromGeneration">The generation to migrate from.</param>
/// <param name="ToGeneration">The generation to migrate to.</param>
/// <param name="Operations">Collection of migration operations.</param>
/// <param name="JmesPath">The JmesPath expression for the migration.</param>
public record EventTypeMigrationDefinition(
    EventTypeGeneration FromGeneration,
    EventTypeGeneration ToGeneration,
    IEnumerable<EventTypeMigrationOperations> Operations,
    JsonObject JmesPath);

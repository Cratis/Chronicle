// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeMigrationsVersion.given;

public class migration_definitions : Specification
{
    protected static EventTypeMigrationDefinition Definition(uint from = 1, string upcast = """{"value":"source"}""") => new(new(from), new(from + 1), [], JsonNode.Parse(upcast)!.AsObject(), new JsonObject());
}

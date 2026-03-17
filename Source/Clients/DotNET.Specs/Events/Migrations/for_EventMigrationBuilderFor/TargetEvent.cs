// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilderFor;

[EventType("TargetEvent", 2)]
public record TargetEvent(string FirstName, string LastName);

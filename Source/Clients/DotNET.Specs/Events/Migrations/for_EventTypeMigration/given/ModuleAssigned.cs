// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

[EventType(generation: 2)]
public record ModuleAssigned(ModuleKey Module);

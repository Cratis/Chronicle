// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

[EventType("TestEvent", 5)]
public record TestEventV5(string FirstName);

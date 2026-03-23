// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

[EventType("TestEvent", 2)]
public record TestEventV2(string FirstName, string LastName, int Age);

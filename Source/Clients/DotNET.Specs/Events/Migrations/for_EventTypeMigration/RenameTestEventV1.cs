// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

[EventType("RenameTestEvent", 1)]
public record RenameTestEventV1(string EmailAddress);

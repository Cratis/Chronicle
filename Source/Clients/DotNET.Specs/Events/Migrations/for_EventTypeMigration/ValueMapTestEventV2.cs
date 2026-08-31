// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

[EventType("ValueMapTestEvent", 2)]
public record ValueMapTestEventV2(ValueMapTestStatus Status);

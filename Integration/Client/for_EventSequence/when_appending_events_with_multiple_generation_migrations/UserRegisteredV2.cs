// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

[EventType("UserRegistered", generation: 2)]
public record UserRegisteredV2(string FirstName, string LastName);

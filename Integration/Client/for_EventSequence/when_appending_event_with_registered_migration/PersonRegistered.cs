// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration;

[EventType(generation: 2)]
public record PersonRegistered(string FirstName, string LastName);

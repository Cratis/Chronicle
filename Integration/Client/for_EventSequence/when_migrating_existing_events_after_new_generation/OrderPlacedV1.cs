// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

[EventType("OrderPlaced", generation: 1)]
public record OrderPlacedV1(string Description);

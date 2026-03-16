// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

/// <summary>
/// Generation 2 of OrderPlaced — adds a Status field with a default value.
/// </summary>
/// <param name="Description">The order description.</param>
/// <param name="Status">The order status.</param>
[EventType(generation: 2)]
public record OrderPlaced(string Description, string Status);

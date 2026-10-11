// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

/// <summary>
/// A public name claim covered by the constraint that applies to every event sequence.
/// </summary>
/// <param name="Name">The claimed name.</param>
[EventType]
[Public]
public record PublicNameClaimedEverywhere(string Name);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

/// <summary>
/// The public counterpart of a name claim forwarded to the outbox.
/// </summary>
/// <param name="Name">The claimed name.</param>
[EventType]
[Public]
public record PublicNameClaimed(string Name);

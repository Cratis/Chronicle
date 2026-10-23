// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Public contract event, which a kernel only accepts on the outbox.
/// </summary>
/// <param name="Member">The member announced.</param>
[EventType]
[Public]
public record MembershipAnnounced(string Member);

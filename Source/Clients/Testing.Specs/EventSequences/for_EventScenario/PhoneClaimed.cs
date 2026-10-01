// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event claiming a contact for an event source, sharing <see cref="ContactClaim.Name"/> with <see cref="EmailClaimed"/>.
/// </summary>
/// <param name="Number">The phone number.</param>
[EventType]
[Unique(ContactClaim.Name, "A contact has already been claimed")]
public record PhoneClaimed(string Number);

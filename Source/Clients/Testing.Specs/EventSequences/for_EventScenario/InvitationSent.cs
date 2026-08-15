// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Claims an <see cref="InvitedEmailAddress"/> for the invitation, so no other invitation can be sent to it while
/// this one is outstanding.
/// </summary>
/// <param name="Address">The address the invitation was sent to.</param>
[EventType]
public record InvitationSent(InvitedEmailAddress Address);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// One of the ways an invitation ends, releasing the <see cref="UniqueInvitedEmailAddress"/> constraint.
/// </summary>
[EventType]
public record InvitationRevoked;

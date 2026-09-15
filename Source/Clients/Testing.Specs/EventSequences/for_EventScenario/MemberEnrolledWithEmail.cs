// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Emitted when a member enrolls, recording the email address they are reachable at. Carries personal data,
/// so it is what the compliance specs append to see whether the harness protects it.
/// </summary>
/// <param name="EmailAddress">The email address the member enrolled with.</param>
[EventType]
public record MemberEnrolledWithEmail(MemberEmailAddress EmailAddress);

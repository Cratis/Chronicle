// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The email address an invitation was sent to, constrained for uniqueness by <see cref="UniqueInvitedEmailAddress"/>.
/// </summary>
/// <param name="Value">The email address.</param>
public record InvitedEmailAddress(string Value) : ConceptAs<string>(Value);

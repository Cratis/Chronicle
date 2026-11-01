// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a registration decision and the active owner it was compared with.
/// </summary>
/// <param name="Outcome">The precedence outcome.</param>
/// <param name="OwnerSequence">The owning event sequence, when there is a conflict.</param>
/// <param name="OwnerVersion">The highest known owning client version.</param>
public record RegistrationPrecedence(RegistrationPrecedenceOutcome Outcome, EventSequenceId? OwnerSequence = null, string? OwnerVersion = null);

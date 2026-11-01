// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a definition's move between event sequences for cursor and failed-partition hand-over.
/// </summary>
/// <param name="ObserverId">The observer that moved.</param>
/// <param name="From">The previous sequence.</param>
/// <param name="To">The new sequence.</param>
/// <param name="Outcome">The registration precedence behind the move.</param>
/// <param name="IncomingVersion">The incoming client's version.</param>
/// <param name="OwnerVersion">The previous owner's version.</param>
internal record DefinitionOwnershipSwitch(ObserverId ObserverId, EventSequenceId From, EventSequenceId To, RegistrationPrecedenceOutcome Outcome, string IncomingVersion, string OwnerVersion);

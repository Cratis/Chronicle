// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event carrying two <c>ConceptAs&lt;Guid&gt;</c> identifiers that together form a composite unique key,
/// used to verify that a unique constraint over more than one ConceptAs-valued property registers and enforces.
/// </summary>
/// <param name="Request">The request the candidate was submitted for.</param>
/// <param name="Consultant">The consultant submitted as a candidate.</param>
[EventType]
public record CandidateSubmittedForRequest(RequestId Request, ConsultantId Consultant);

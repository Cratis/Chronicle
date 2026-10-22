// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>An explicit request to advance a floor; the target revision is ignored.</summary>
/// <param name="Target">The requested floor.</param>
/// <param name="ExpectedRevision">The expected revision, or zero for an absent floor.</param>
/// <param name="RequestedBy">The requester for the audit trail.</param>
/// <param name="Reason">The reason for advancement.</param>
/// <param name="Evidence">Membership diagnostics; the advancement service independently rechecks membership.</param>
public record GenerationFloorAdvance(GenerationFloor Target, ulong ExpectedRevision, string RequestedBy, string Reason, GenerationMembershipEvidence Evidence);

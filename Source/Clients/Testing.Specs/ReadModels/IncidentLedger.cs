// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a <see cref="Severity"/> enum payload value (children on the
/// same event source as the parent), used to verify the in-memory harness materializes enum-keyed child
/// collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Incidents">Incident lines keyed by <see cref="IncidentRecorded.Level"/> (a <see cref="Severity"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record IncidentLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<IncidentRecorded>(key: nameof(IncidentRecorded.Level))]
    IEnumerable<IncidentLine> Incidents);

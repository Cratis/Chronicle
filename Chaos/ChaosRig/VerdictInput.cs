// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Collects the independent observations used by the consistency oracle.
/// </summary>
/// <param name="Ledger">The durable append intentions.</param>
/// <param name="Events">The persisted event log.</param>
/// <param name="Observers">The reported observer progress.</param>
/// <param name="ExpectedObservers">The required observer identifiers.</param>
/// <param name="FailedPartitions">The failed partition count.</param>
/// <param name="InFlightPartitions">The persisted in-flight partition count.</param>
/// <param name="PendingJobs">The unfinished job count.</param>
/// <param name="Projections">The projection model states.</param>
/// <param name="Reductions">The reducer model states.</param>
/// <param name="SideLedger">The reactor delivery identifiers.</param>
/// <param name="ExpectedStateDocuments">The persisted observer document count.</param>
/// <param name="Settled">Whether repeated clean snapshots established a stable drain.</param>
internal sealed record VerdictInput(
    IReadOnlyList<LedgerEntry> Ledger,
    IReadOnlyList<ObservedEvent> Events,
    IReadOnlyList<ObserverValue> Observers,
    IReadOnlyList<string> ExpectedObservers,
    int FailedPartitions,
    int InFlightPartitions,
    int PendingJobs,
    IReadOnlyList<ModelValue> Projections,
    IReadOnlyList<ModelValue> Reductions,
    IReadOnlyList<string> SideLedger,
    int ExpectedStateDocuments,
    bool Settled = true);

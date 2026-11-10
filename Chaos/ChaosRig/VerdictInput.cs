// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

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

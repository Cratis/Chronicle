// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Internal authority and immutable acquisition snapshot for one Enforce commit.
/// </summary>
/// <param name="Target">The full target captured by the unit.</param>
/// <param name="Contributions">The acquisitions captured under the enrollment lock.</param>
/// <param name="Resolved">Receives the actual conditions sent by the trusted append.</param>
internal sealed record ReadProtectionCommit(
    ReadProtectionTarget Target,
    IReadOnlyList<ReadProtectionContribution> Contributions,
    Action<IReadOnlyList<AppendCondition>> Resolved);

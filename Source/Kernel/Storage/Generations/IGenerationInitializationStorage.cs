// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.Generations;

/// <summary>
/// Stores first-write-wins receipts and retained blocker audit records.
/// </summary>
public interface IGenerationInitializationStorage
{
    /// <summary>
    /// Reads all receipts without creating storage.
    /// </summary>
    /// <returns>The receipts.</returns>
    Task<IReadOnlyList<GenerationInitializationReceipt>> GetReceipts();

    /// <summary>
    /// Records a receipt only when its step and subject have no receipt.
    /// </summary>
    /// <param name="receipt">The receipt.</param>
    /// <returns>Whether this call recorded the receipt.</returns>
    Task<bool> TryRecord(GenerationInitializationReceipt receipt);

    /// <summary>
    /// Reads all blockers, including resolved blockers.
    /// </summary>
    /// <returns>The blockers.</returns>
    Task<IReadOnlyList<GenerationInitializationBlocker>> GetBlockers();

    /// <summary>
    /// Records a blocker, preserving the raised time while unresolved.
    /// </summary>
    /// <param name="blocker">The blocker.</param>
    /// <returns>The asynchronous operation.</returns>
    Task RecordBlocker(GenerationInitializationBlocker blocker);

    /// <summary>
    /// Resolves a blocker without deleting its audit record.
    /// </summary>
    /// <param name="kind">The reason.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="at">The resolution time.</param>
    /// <returns>The asynchronous operation.</returns>
    Task ResolveBlocker(GenerationInitializationBlockerKind kind, string subject, DateTimeOffset at);
}

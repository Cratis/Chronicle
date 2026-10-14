// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

/// <summary>
/// Owns namespace-wide receipt identities shared by all event sequences in that namespace.
/// </summary>
public class EffectReceiptState
{
    /// <summary>
    /// Gets the namespace-wide atomic write lock.
    /// </summary>
    internal object Sync { get; } = new();

    /// <summary>
    /// Gets the first-writer-wins receipt identities in this process scope.
    /// </summary>
    internal Dictionary<string, EffectReceipt> Receipts { get; } = new(StringComparer.Ordinal);
}

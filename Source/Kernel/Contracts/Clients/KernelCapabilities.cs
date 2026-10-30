// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Identifies optional kernel behaviors advertised during connection.
/// </summary>
public static class KernelCapabilities
{
    /// <summary>
    /// The kernel accepts precise schemas for defaulted event properties without changing their generation.
    /// </summary>
    public const string PreciseEventTypeSchemas = "precise-event-type-schemas";

    /// <summary>
    /// The kernel accepts event-driven closing and reopening constraint definitions.
    /// </summary>
    public const string ClosesStreamConstraints = "closes-stream-constraints";

    /// <summary>
    /// The kernel atomically records occurrence effects and their opaque responses with event batches.
    /// </summary>
    public const string EffectReceipts = "effect-receipts";

    /// <summary>
    /// The kernel records and delivers application occurrences under durable fenced leases.
    /// </summary>
    public const string Occurrences = "occurrences";

    /// <summary>
    /// The kernel validates several ordered guards for the same event source atomically.
    /// </summary>
    public const string ConcurrencyScopeConjunctions = "concurrency-scope-conjunctions";

    /// <summary>
    /// The kernel settles keyed appends against durable idempotency identities through the AppendOnce and AppendManyOnce operations.
    /// </summary>
    public const string IdempotentAppends = "idempotent-appends";

    /// <summary>
    /// The kernel supports structured composite-key reads of materialized read models.
    /// </summary>
    public const string CompositeKeyLookups = "composite-key-lookups";
}

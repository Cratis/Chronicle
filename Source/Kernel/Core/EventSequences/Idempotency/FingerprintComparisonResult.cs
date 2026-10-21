// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// A settlement outcome retaining the original event type even if its stored event was redacted.
/// </summary>
/// <param name="Status">The settlement outcome.</param>
/// <param name="StoredEventType">The event type in the persistent fingerprint.</param>
/// <param name="Reason">The conflict reason, if any.</param>
/// <param name="UnverifiableKind">The first unverifiable protection kind in persistent format order.</param>
internal record FingerprintComparisonResult(FingerprintComparisonStatus Status, EventType StoredEventType, FingerprintConflictReason? Reason = null, ProtectionKind? UnverifiableKind = null);

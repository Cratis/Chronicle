// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Supplies a worker with the immutable payload and current fencing token.
/// </summary>
/// <param name="Id">The occurrence identity.</param>
/// <param name="Trigger">The trigger identity.</param>
/// <param name="Kind">The ingress kind.</param>
/// <param name="Payload">The admitted JSON.</param>
/// <param name="SchemaRevision">The admitted schema revision.</param>
/// <param name="RecordedAt">The kernel recording time.</param>
/// <param name="Due">The scheduled instant, if any.</param>
/// <param name="Attempts">The attempt number.</param>
/// <param name="Epoch">The fencing token epoch.</param>
/// <param name="LeaseExpiry">The kernel lease expiry.</param>
public record OccurrenceClaimDetails(string Id, string Trigger, OccurrenceKind Kind, string Payload, long SchemaRevision, DateTimeOffset RecordedAt, DateTimeOffset? Due, int Attempts, long Epoch, DateTimeOffset LeaseExpiry);

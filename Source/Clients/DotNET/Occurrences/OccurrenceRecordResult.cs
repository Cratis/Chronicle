// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Describes the durable ingress outcome. Retrying the same identity and payload after a lost acknowledgement is safe.
/// </summary>
/// <param name="Id">The occurrence identity.</param>
/// <param name="Status">The ingress outcome.</param>
public record OccurrenceRecordResult(OccurrenceId Id, OccurrenceRecordStatus Status);

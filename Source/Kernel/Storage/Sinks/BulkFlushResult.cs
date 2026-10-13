// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Confirms the writes drained from a bulk window without closing it.
/// </summary>
/// <param name="WindowId">The open window, or null for a sink that writes directly.</param>
/// <param name="FailedPartitions">The unreported buffered write failures.</param>
public record BulkFlushResult(BulkWindowId? WindowId, IEnumerable<FailedPartition> FailedPartitions);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the result of waiting for observers affected by an append operation.
/// </summary>
/// <param name="IsSuccess">Whether all affected observers completed successfully.</param>
/// <param name="FailedPartitions">Collection of failed partitions discovered while waiting.</param>
public record AppendResultWaitForCompletionResult(bool IsSuccess, IEnumerable<FailedPartition> FailedPartitions);

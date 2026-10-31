// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the outcome of asking an observer to retry its failed partitions.
/// </summary>
/// <param name="FailedPartitions">The number of failed partitions the observer had when it was asked.</param>
public record RetryFailedPartitionsResult(int FailedPartitions);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Represents the unique identifier of a failed partition.
/// </summary>
/// <param name="value">The inner value.</param>
public record FailedPartitionId(Guid value) : ConceptAs<Guid>(value)
{
    /// <summary>
    /// Creates a new instance of the <see cref="FailedPartitionId"/> class with a new unique value.
    /// </summary>
    /// <returns><see cref="FailedPartitionId"/> instance.</returns>
    public static FailedPartitionId New() => new(Guid.NewGuid());
}

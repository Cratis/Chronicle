// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents the request for retrying a failed partition in an observer.
/// </summary>
[ProtoContract]
public class RetryPartitionRequest
{
    /// <summary>
    /// Gets or sets the microservice identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [ProtoMember(2)]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    public Guid ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the partition to rewind.
    /// </summary>
    public string Partition { get; set; }
}

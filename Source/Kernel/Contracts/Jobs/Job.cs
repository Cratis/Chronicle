// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents the state of a job.
/// </summary>
[ProtoContract]
public class Job
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    [ProtoMember(1)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the details for a job.
    /// </summary>
    [ProtoMember(2)]
    public string Details { get; set; }

    /// <summary>
    /// Gets or sets the type of the job.
    /// </summary>
    [ProtoMember(3)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the status of the job.
    /// </summary>
    [ProtoMember(4)]
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when job was created.
    /// </summary>
    [ProtoMember(5)]
    public SerializableDateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IList<JobStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public JobProgress Progress { get; set; } = new();
}

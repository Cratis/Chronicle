// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents the state of a job step.
/// </summary>
[ProtoContract]
public class JobStep
{
    /// <summary>
    /// Gets or sets the unique identifier for the job step.
    /// </summary>
    [ProtoMember(1)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the job step.
    /// </summary>
    [ProtoMember(2)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the job step.
    /// </summary>
    [ProtoMember(3)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobStepStatus"/>.
    /// </summary>
    [ProtoMember(4)]
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the collection of <see cref="JobStepProgress"/>.
    /// </summary>
    [ProtoMember(5)]
    public IEnumerable<JobStepStatusChanged> StatusChanges { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JobStepProgress"/>.
    /// </summary>
    [ProtoMember(6)]
    public JobStepProgress Progress { get; set; }
}

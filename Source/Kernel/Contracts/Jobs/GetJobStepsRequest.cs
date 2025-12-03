// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents the request for getting job steps for a specific job.
/// </summary>
[ProtoContract]
public class GetJobStepsRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the job id.
    /// </summary>
    [ProtoMember(3)]
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets what statuses to look for. If none are specified, steps for all statuses are returned.
    /// </summary>
    public IEnumerable<JobStepStatus> Statuses { get; set; }
}

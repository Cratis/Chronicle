// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents progress of a job.
/// </summary>
[ProtoContract]
public class JobProgress
{
    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    [ProtoMember(1)]
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the completed number of steps.
    /// </summary>
    [ProtoMember(2)]
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Gets or sets the failed number of steps.
    /// </summary>
    [ProtoMember(3)]
    public int FailedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of stopped steps.
    /// </summary>
    [ProtoMember(4)]
    public int StoppedSteps { get; set; }

    /// <summary>
    /// Gets whether or not the job is completed.
    /// </summary>
    [ProtoMember(5)]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets whether or not the job is completed.
    /// </summary>
    [ProtoMember(6)]
    public bool IsStopped { get; set; }

    /// <summary>
    /// Gets or sets the current message associated with the progress.
    /// </summary>
    [ProtoMember(7)]
    public string Message { get; set; }
}

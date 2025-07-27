// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Represents the progress of a step.
/// </summary>
[ProtoContract]
public class JobStepProgress
{
    /// <summary>
    /// Gets or sets the percentage of the step.
    /// </summary>
    [ProtoMember(1)]
    public int Percentage { get; set; }

    /// <summary>
    /// Gets or sets the current message associated with the progress.
    /// </summary>
    [ProtoMember(2)]
    public string Message { get; set; }
}

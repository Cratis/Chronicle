// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the unique identifier for a job.
/// </summary>
/// <param name="Value">Inner value.</param>
public record JobId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Get the job id that is not set.
    /// </summary>
    public static readonly JobId NotSet = new(Guid.Empty);

    public static implicit operator Guid(JobId jobId) => jobId.Value;
    public static implicit operator JobId(Guid value) => new(value);
}

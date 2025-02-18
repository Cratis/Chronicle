// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Represents the JobType attribute that can be placed on job type classes to specify the <see cref="JobType"/> name.
/// </summary>
/// <param name="jobType">The job type name to use.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class JobTypeAttribute(string jobType) : Attribute
{
    /// <summary>
    /// Gets the <see cref="JobType"/>.
    /// </summary>
    public JobType JobType { get; } = new(jobType);
}

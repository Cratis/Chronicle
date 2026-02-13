// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The error when resuming a job.
/// </summary>
[GenerateOneOf]
public partial class ResumeJobError : OneOfBase<CannotResumeJobError, FailedResumingJobSteps>;
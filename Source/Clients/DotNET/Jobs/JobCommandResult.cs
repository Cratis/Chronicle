// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the outcome of asking a job to stop or resume.
/// </summary>
/// <param name="Accepted">Whether the job took the request.</param>
/// <param name="Refusal">The reason the request was refused, <see cref="JobCommandRefusal.NotRefused"/> when it was accepted.</param>
public record JobCommandResult(bool Accepted, JobCommandRefusal Refusal);

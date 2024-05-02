// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Jobs;

/// <summary>
/// Represents the progress of a job step.
/// </summary>
/// <param name="Percentage">The percentage of the step.</param>
/// <param name="Message">The current message associated with the progress.</param>
public record JobStepProgress(int Percentage, string Message);

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the progress of a step.
/// </summary>
/// <param name="Percentage">Percentage of the step.</param>
/// <param name="Message">Message of current progress.</param>
public record StepProgress(StepPercentage Percentage, StepProgressMessage Message);

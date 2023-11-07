// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Defines a system that is capable of evaluating whether or not an observer should replay or not.
/// </summary>
/// <remarks>
/// In addition to evaluating, this system is also responsible for possibly scheduling or surfacing
/// potential replay jobs.
/// </remarks>
public interface IReplayEvaluator
{
    /// <summary>
    /// Evaluate whether or not an observer should replay or not. It will not perform a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayEvaluationContext"/> to evaluate.</param>
    /// <returns>True if it should replay, false if not.</returns>
    Task<bool> Evaluate(ReplayEvaluationContext context);
}

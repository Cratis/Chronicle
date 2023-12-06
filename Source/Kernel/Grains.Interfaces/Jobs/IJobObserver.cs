// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Defines a system for observing job events.
/// </summary>
public interface IJobObserver : IGrainObserver
{
    /// <summary>
    /// Called when the job has stopped.
    /// </summary>
    void OnJobStopped();

    /// <summary>
    /// Called when the job has paused.
    /// </summary>
    void OnJobPaused();
}

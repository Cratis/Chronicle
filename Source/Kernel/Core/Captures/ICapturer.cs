// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the grain running a single capture on its schedule.
/// The grain is keyed by the capture identifier with the event store name as key extension.
/// </summary>
public interface ICapturer : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Start capturing - schedules capture cycles at the capture's poll interval using a persistent reminder,
    /// so the schedule survives kernel restarts.
    /// </summary>
    /// <param name="capture">The <see cref="Capture"/> to run.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(Capture capture);

    /// <summary>
    /// Stop capturing - unregisters the schedule.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Stop();
}

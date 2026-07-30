// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the grain managing the captures of an event store - starting, stopping and resuming them across kernel restarts.
/// The grain is keyed by the name of the event store.
/// </summary>
public interface ICapturesManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the manager is running and that every started capture has its capturer running.
    /// Called when the kernel starts, making started captures survive restarts.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Start a capture. Once started it captures on its schedule and can not be changed until stopped.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture to start.</param>
    /// <returns>Validation messages preventing the start - empty when the capture was started.</returns>
    Task<IEnumerable<CaptureValidationMessage>> Start(CaptureId captureId);

    /// <summary>
    /// Stop a capture.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture to stop.</param>
    /// <returns>Awaitable task.</returns>
    Task Stop(CaptureId captureId);

    /// <summary>
    /// Delete a capture, stopping it first if it is running.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(CaptureId captureId);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the command for stopping a capture.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="CaptureId">The unique identifier of the capture.</param>
[Command]
public record StopCapture(string EventStore, string CaptureId)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(ICapturesService captures) =>
        captures.Stop(new()
        {
            EventStore = EventStore,
            Id = CaptureId
        });
}

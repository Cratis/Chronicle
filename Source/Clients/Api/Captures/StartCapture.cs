// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the command for starting a capture. Once started it captures on its schedule and
/// can not be changed until stopped.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="CaptureId">The unique identifier of the capture.</param>
[Command]
public record StartCapture(string EventStore, string CaptureId)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <returns>The <see cref="StartCaptureResult"/> - empty messages means the capture was started.</returns>
    internal async Task<StartCaptureResult> Handle(ICapturesService captures)
    {
        var response = await captures.Start(new()
        {
            EventStore = EventStore,
            Id = CaptureId
        });

        return new() { Messages = response.Messages.ToApi() };
    }
}

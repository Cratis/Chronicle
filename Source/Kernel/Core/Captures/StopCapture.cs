// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the command for stopping a running capture.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="CaptureId">The unique identifier of the capture.</param>
[Command]
[BelongsTo(WellKnownServices.Captures)]
public record StopCapture(EventStoreName EventStore, Concepts.Captures.CaptureId CaptureId)
{
    /// <summary>
    /// Handles the command by asking the captures manager grain to stop the capture.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the captures manager with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory) =>
        grainFactory.GetGrain<ICapturesManager>(EventStore).Stop(CaptureId);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the command for starting a capture.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="CaptureId">The unique identifier of the capture.</param>
[Command]
[BelongsTo(WellKnownServices.Captures)]
public record StartCapture(EventStoreName EventStore, Concepts.Captures.CaptureId CaptureId)
{
    /// <summary>
    /// Handles the command by asking the captures manager grain to start the capture.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the captures manager with.</param>
    /// <returns>What validating the capture had to say before it started.</returns>
    public async Task<StartCaptureResult> Handle(IGrainFactory grainFactory)
    {
        var messages = await grainFactory.GetGrain<ICapturesManager>(EventStore).Start(CaptureId);
        return new(messages.ToContract());
    }
}

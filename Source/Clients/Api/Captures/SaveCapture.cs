// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the command for saving a capture. The capture's name is derived from the declaration.
/// A capture that is started can not be changed and is rejected with a message.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="Id">The unique identifier of the capture - empty to create a new capture.</param>
/// <param name="Declaration">The capture declaration language source text.</param>
[Command]
public record SaveCapture(string EventStore, string Id, string Declaration)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <returns>The <see cref="SaveCaptureResult"/>.</returns>
    internal async Task<SaveCaptureResult> Handle(ICapturesService captures)
    {
        var response = await captures.Save(new()
        {
            EventStore = EventStore,
            Id = Id,
            Declaration = Declaration
        });

        return new()
        {
            Capture = response.Capture?.ToApi(),
            Messages = response.Messages.ToApi()
        };
    }
}

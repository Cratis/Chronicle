// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ModelBinding;
using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the API for working with capture commands.
/// </summary>
[Route("/api/event-store/{eventStore}/captures")]
public class CaptureCommands : ControllerBase
{
    readonly ICapturesService _captures;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureCommands"/> class.
    /// </summary>
    /// <param name="captures"><see cref="ICapturesService"/> for working with captures.</param>
    internal CaptureCommands(ICapturesService captures)
    {
        _captures = captures;
    }

    /// <summary>
    /// Save a capture. A capture that is started can not be changed and is rejected with a message.
    /// </summary>
    /// <param name="command">Command for saving the capture.</param>
    /// <returns>The <see cref="SaveCaptureResult"/>.</returns>
    [HttpPost("save")]
    public async Task<SaveCaptureResult> Save(
        [FromRequest] SaveCapture command)
    {
        var response = await _captures.Save(new()
        {
            EventStore = command.EventStore,
            Id = command.Id,
            Declaration = command.Declaration
        });

        return new()
        {
            Capture = response.Capture?.ToApi(),
            Messages = response.Messages.ToApi()
        };
    }

    /// <summary>
    /// Delete a capture, stopping it first if it is running.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="captureId">The identifier of the capture to delete.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{captureId}/delete")]
    public Task Delete(
        [FromRoute] string eventStore,
        [FromRoute] string captureId) =>
        _captures.Delete(new()
        {
            EventStore = eventStore,
            Id = captureId
        });

    /// <summary>
    /// Start a capture. Once started it captures on its schedule and can not be changed until stopped.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="captureId">The identifier of the capture to start.</param>
    /// <returns>The <see cref="StartCaptureResult"/> - empty messages means the capture was started.</returns>
    [HttpPost("{captureId}/start")]
    public async Task<StartCaptureResult> Start(
        [FromRoute] string eventStore,
        [FromRoute] string captureId)
    {
        var response = await _captures.Start(new()
        {
            EventStore = eventStore,
            Id = captureId
        });

        return new() { Messages = response.Messages.ToApi() };
    }

    /// <summary>
    /// Stop a capture.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="captureId">The identifier of the capture to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{captureId}/stop")]
    public Task Stop(
        [FromRoute] string eventStore,
        [FromRoute] string captureId) =>
        _captures.Stop(new()
        {
            EventStore = eventStore,
            Id = captureId
        });

    /// <summary>
    /// Validate a capture declaration - compiling it and verifying what it references, such as
    /// external services and event types.
    /// </summary>
    /// <param name="command">Command holding the declaration to validate.</param>
    /// <returns>The messages - empty when the declaration is valid.</returns>
    [HttpPost("validate")]
    public async Task<IEnumerable<CaptureValidationMessage>> ValidateDeclaration(
        [FromRequest] ValidateCaptureDeclaration command)
    {
        var response = await _captures.ValidateDeclaration(new()
        {
            EventStore = command.EventStore,
            Declaration = command.Declaration
        });

        return response.Messages.ToApi();
    }
}

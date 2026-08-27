// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Capture = Cratis.Chronicle.Concepts.Captures.Capture;
using CaptureStatus = Cratis.Chronicle.Concepts.Captures.CaptureStatus;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the command for saving a capture, deriving its name from the declaration.
/// </summary>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="Id">The unique identifier of the capture, empty to create a new one.</param>
/// <param name="Declaration">The capture declaration language source text.</param>
/// <remarks>
/// A started capture cannot be changed. It is rejected with a message rather than an error, because the caller
/// is an editor and the answer is something to show in it.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Captures)]
public record SaveCapture(EventStoreName EventStore, Concepts.Captures.CaptureId Id, string Declaration)
{
    /// <summary>
    /// Handles the command by compiling the declaration and saving the capture it describes.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the captures.</param>
    /// <param name="languageService">The <see cref="ILanguageService"/> to compile the declaration with.</param>
    /// <param name="captureValidator">The <see cref="ICaptureValidator"/> to validate the compiled capture with.</param>
    /// <returns>The saved capture, or the messages saying why it was not saved.</returns>
    internal async Task<SaveCaptureResult> Handle(
        IStorage storage,
        ILanguageService languageService,
        ICaptureValidator captureValidator)
    {
        var captures = storage.GetEventStore(EventStore).Captures;
        var captureId = CaptureConverters.ResolveCaptureId(Id);

        if (await captures.Has(captureId))
        {
            var existing = await captures.Get(captureId);
            if (existing.Status == CaptureStatus.Started)
            {
                return new(null, [new() { Message = "The capture is started - stop it before changing it" }]);
            }
        }

        var compilation = languageService.Compile(Declaration);
        return await compilation.Match(
            async definition =>
            {
                var capture = new Capture(captureId, definition.Name, Declaration, CaptureStatus.Stopped);
                await captures.Save(capture);
                var messages = await captureValidator.Validate(EventStore, definition with { Id = captureId });
                return new SaveCaptureResult(capture.ToReadModel(), messages.ToContract());
            },
            errors => Task.FromResult(new SaveCaptureResult(null, CaptureConverters.ToContract(errors.Errors))));
    }
}

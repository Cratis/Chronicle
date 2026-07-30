// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICapturesManager"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing captures.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for compiling capture declarations.</param>
/// <param name="captureValidator"><see cref="ICaptureValidator"/> for validating compiled captures.</param>
/// <param name="logger">The logger.</param>
public class CapturesManager(
    IStorage storage,
    ILanguageService languageService,
    ICaptureValidator captureValidator,
    ILogger<CapturesManager> logger) : Grain, ICapturesManager
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    ICapturesStorage Captures => storage.GetEventStore(_eventStoreName).Captures;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Ensure()
    {
        var captures = await Captures.GetAll();
        foreach (var capture in captures.Where(capture => capture.Status == CaptureStatus.Started))
        {
            try
            {
                await GetCapturer(capture.Id).Start(capture);
            }
            catch (Exception exception)
            {
                logger.FailedResumingCapture(exception, capture.Name, capture.Id);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CaptureValidationMessage>> Start(CaptureId captureId)
    {
        if (!await Captures.Has(captureId))
        {
            return [new CaptureValidationMessage("The capture does not exist")];
        }

        var capture = await Captures.Get(captureId);
        if (capture.Status == CaptureStatus.Started)
        {
            return [];
        }

        var compilation = languageService.Compile(capture.Declaration);
        var messages = compilation.Match(
            definition => captureValidator.Validate(_eventStoreName, definition with { Id = captureId }),
            errors => Task.FromResult(errors.Errors.Select(error => new CaptureValidationMessage(error.Message, error.Line, error.Column))));

        var messagesResolved = (await messages).ToArray();
        if (messagesResolved.Length > 0)
        {
            return messagesResolved;
        }

        capture = capture with { Status = CaptureStatus.Started };
        await Captures.Save(capture);
        await GetCapturer(captureId).Start(capture);
        return [];
    }

    /// <inheritdoc/>
    public async Task Stop(CaptureId captureId)
    {
        if (!await Captures.Has(captureId))
        {
            return;
        }

        await GetCapturer(captureId).Stop();

        var capture = await Captures.Get(captureId);
        if (capture.Status != CaptureStatus.Stopped)
        {
            await Captures.Save(capture with { Status = CaptureStatus.Stopped });
        }
    }

    /// <inheritdoc/>
    public async Task Delete(CaptureId captureId)
    {
        await GetCapturer(captureId).Stop();
        await Captures.Delete(captureId);
    }

    ICapturer GetCapturer(CaptureId captureId) => GrainFactory.GetGrain<ICapturer>(captureId.Value, _eventStoreName.Value);
}

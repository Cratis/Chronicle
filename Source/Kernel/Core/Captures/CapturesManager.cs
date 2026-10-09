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
/// <param name="eventsSubscriptions"><see cref="ICaptureEventsSubscriptions"/> for running captures that read from an inbox.</param>
/// <param name="logger">The logger.</param>
public class CapturesManager(
    IStorage storage,
    ILanguageService languageService,
    ICaptureValidator captureValidator,
    ICaptureEventsSubscriptions eventsSubscriptions,
    ILogger<CapturesManager> logger) : Grain, ICapturesManager
{
    /// <summary>
    /// How often a started events capture is checked for a missing subscription. A grain deactivation or a failed
    /// setup drops the in-memory subscription of an observer, and nothing else would put it back.
    /// </summary>
    internal static readonly TimeSpan ReconcileInterval = TimeSpan.FromMinutes(1);

    EventStoreName _eventStoreName = EventStoreName.NotSet;

    ICapturesStorage Captures => storage.GetEventStore(_eventStoreName).Captures;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();

        // Started events captures are observers whose subscription lives in memory only, so the manager keeps
        // reconciling them for as long as it is active - it is what brings them back after a deactivation.
        this.RegisterGrainTimer(Reconcile, new GrainTimerCreationOptions
        {
            DueTime = ReconcileInterval,
            Period = ReconcileInterval,
            Interleave = true
        });
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
                await Run(capture);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Not swallowed: it is logged as an error and the reconciliation timer keeps retrying, so a
                // started capture never stays silently unsubscribed.
                logger.FailedResumingCapture(exception, capture.Name, capture.Id);
            }
        }
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreNamespaceName @namespace)
    {
        foreach (var (capture, definition) in await GetStartedEventsCaptures())
        {
            try
            {
                await eventsSubscriptions.Subscribe(_eventStoreName, @namespace, definition);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.FailedSubscribingCaptureInNamespace(exception, capture.Name, capture.Id, @namespace);
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
        if (GetEventsDefinition(capture) is not null)
        {
            // Subscribe first: a capture is only recorded as started once it is subscribed in every namespace.
            // A failed subscription rolls itself back and is thrown, leaving the capture stopped.
            await Run(capture);
            await Captures.Save(capture);
            return [];
        }

        await Captures.Save(capture);
        await Run(capture);
        return [];
    }

    /// <inheritdoc/>
    public async Task Stop(CaptureId captureId)
    {
        if (!await Captures.Has(captureId))
        {
            return;
        }

        var capture = await Captures.Get(captureId);
        await StopRunning(capture);
        if (capture.Status != CaptureStatus.Stopped)
        {
            await Captures.Save(capture with { Status = CaptureStatus.Stopped });
        }
    }

    /// <inheritdoc/>
    public async Task Delete(CaptureId captureId)
    {
        var definition = await Captures.Has(captureId) ? GetEventsDefinition(await Captures.Get(captureId)) : null;
        if (definition is not null)
        {
            // Removes the observers, their offsets and definition and the state kept per namespace too.
            await eventsSubscriptions.Remove(_eventStoreName, definition);
        }
        else
        {
            await GetCapturer(captureId).Stop();
        }

        await Captures.Delete(captureId);
    }

    async Task Reconcile(CancellationToken cancellationToken)
    {
        foreach (var (capture, definition) in await GetStartedEventsCaptures())
        {
            try
            {
                await eventsSubscriptions.Recover(_eventStoreName, definition);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.FailedRecoveringCapture(exception, capture.Name, capture.Id);
            }
        }
    }

    async Task<IEnumerable<(Capture Capture, CaptureDefinition Definition)>> GetStartedEventsCaptures() =>
        (await Captures.GetAll())
            .Where(capture => capture.Status == CaptureStatus.Started)
            .Select(capture => (Capture: capture, Definition: GetEventsDefinition(capture)))
            .Where(pair => pair.Definition is not null)
            .Select(pair => (pair.Capture, pair.Definition!))
            .ToArray();

    CaptureDefinition? GetEventsDefinition(Capture capture)
    {
        var compilation = languageService.Compile(capture.Declaration);
        var definition = compilation.Match(definition => (CaptureDefinition?)(definition with { Id = capture.Id }), _ => null);
        return definition?.Source.Type == SourceType.Events ? definition : null;
    }

    async Task Run(Capture capture)
    {
        var definition = GetEventsDefinition(capture);
        if (definition is not null)
        {
            // An events capture is an observer of its inbox, not a poll loop.
            await eventsSubscriptions.Subscribe(_eventStoreName, definition);
            return;
        }

        await GetCapturer(capture.Id).Start(capture);
    }

    async Task StopRunning(Capture capture)
    {
        var definition = GetEventsDefinition(capture);
        if (definition is not null)
        {
            await eventsSubscriptions.Unsubscribe(_eventStoreName, definition);
            return;
        }

        await GetCapturer(capture.Id).Stop();
    }

    ICapturer GetCapturer(CaptureId captureId) => GrainFactory.GetGrain<ICapturer>(captureId.Value, _eventStoreName.Value);
}

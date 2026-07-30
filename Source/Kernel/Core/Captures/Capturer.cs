// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICapturer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing captures and event types.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for compiling the capture declaration.</param>
/// <param name="sourceReaders"><see cref="IInstancesOf{T}"/> of <see cref="ICaptureSourceReader"/> for reading the source.</param>
/// <param name="changeDetector"><see cref="ICaptureChangeDetector"/> for detecting changes between observations.</param>
/// <param name="whenClauseEvaluator"><see cref="IWhenClauseEvaluator"/> for matching changes against when clauses.</param>
/// <param name="contentMapper"><see cref="ICaptureContentMapper"/> for mapping changes to event content.</param>
/// <param name="logger">The logger.</param>
public class Capturer(
    IStorage storage,
    ILanguageService languageService,
    IInstancesOf<ICaptureSourceReader> sourceReaders,
    ICaptureChangeDetector changeDetector,
    IWhenClauseEvaluator whenClauseEvaluator,
    ICaptureContentMapper contentMapper,
    ILogger<Capturer> logger) : Grain, ICapturer, IRemindable
{
    const string CycleReminder = "capture-cycle";

    EventStoreName _eventStoreName = EventStoreName.NotSet;
    CaptureId _captureId = CaptureId.NotSet;
    Capture? _capture;
    CaptureDefinition? _definition;

    ICapturesStorage Captures => storage.GetEventStore(_eventStoreName).Captures;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _captureId = this.GetPrimaryKey(out var keyExtension);
        _eventStoreName = keyExtension ?? EventStoreName.NotSet;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(Capture capture)
    {
        logger.StartingCapture(capture.Name, _captureId);
        _capture = capture;
        _definition = null;
        TryCompile();

        var interval = ResolveInterval();
        await this.RegisterOrUpdateReminder(CycleReminder, interval, interval);

        if (_definition is not null)
        {
            await RunCycle();
        }
    }

    /// <inheritdoc/>
    public async Task Stop()
    {
        logger.StoppingCapture(_captureId);
        var reminder = await this.GetReminder(CycleReminder);
        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }

        _capture = null;
        _definition = null;
    }

    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName != CycleReminder)
        {
            return;
        }

        if (_capture is null && !await TryRecover())
        {
            return;
        }

        if (_definition is null)
        {
            TryCompile();
            if (_definition is null)
            {
                return;
            }
        }

        await RunCycle();
    }

    /// <summary>
    /// Recover the capture after a kernel restart - the persistent reminder fires without the grain having
    /// been started explicitly, so the capture is reloaded from storage and resumed if it is still started.
    /// </summary>
    /// <returns>True when the capture was recovered and should run, false when not.</returns>
    async Task<bool> TryRecover()
    {
        if (!await Captures.Has(_captureId))
        {
            await Stop();
            return false;
        }

        var capture = await Captures.Get(_captureId);
        if (capture.Status != CaptureStatus.Started)
        {
            await Stop();
            return false;
        }

        _capture = capture;
        return true;
    }

    void TryCompile()
    {
        var result = languageService.Compile(_capture!.Declaration);
        result.Switch(
            definition => _definition = definition with { Id = _captureId },
            errors => logger.CaptureDeclarationInvalid(_capture.Name, _captureId, string.Join(", ", errors.Errors.Select(error => error.Message))));
    }

    TimeSpan ResolveInterval() =>
        _definition is not null && CapturePollInterval.TryParse(_definition.Source.Poll, out var interval)
            ? interval
            : CapturePollInterval.Minimum;

    async Task RunCycle()
    {
        try
        {
            var definition = _definition!;
            var capture = _capture!;

            var reader = sourceReaders.FirstOrDefault(reader => reader.Type == definition.Source.Type)
                ?? throw new UnsupportedCaptureCapability($"'{definition.Source.Type}' sources are not supported by the capturing engine yet");

            var items = await reader.Read(_eventStoreName, definition.Source);
            var current = KeyItems(items, definition.KeyProperty);

            var observation = await Captures.GetObservation(_captureId);
            var previous = observation.Items.ToDictionary(
                item => item.Key,
                item => JsonNode.Parse(item.Content)!.AsObject());

            var changes = changeDetector.Detect(previous, current);
            var events = await BuildEvents(definition, capture, changes);

            if (events.Count > 0)
            {
                await Append(capture, events);
            }

            await Captures.SaveObservation(new CaptureObservation(
                _captureId,
                current.Select(kvp => new CaptureObservedItem(kvp.Key, kvp.Value.ToJsonString())).ToList()));
        }
        catch (Exception exception)
        {
            logger.CaptureCycleFailed(exception, _capture?.Name ?? CaptureName.NotSet, _captureId);
        }
    }

    Dictionary<string, JsonObject> KeyItems(IEnumerable<JsonObject> items, string keyProperty)
    {
        var keyed = new Dictionary<string, JsonObject>();
        foreach (var item in items)
        {
            var key = CaptureItemPath.Resolve(item, keyProperty)?.ToString();
            if (string.IsNullOrEmpty(key))
            {
                logger.SkippingItemWithoutKey(_captureId, keyProperty);
                continue;
            }

            keyed[key] = item;
        }

        return keyed;
    }

    async Task<List<EventToAppend>> BuildEvents(CaptureDefinition definition, Capture capture, IEnumerable<CaptureChange> changes)
    {
        var events = new List<EventToAppend>();
        var tags = CaptureTags.For(capture.Name).ToArray();

        foreach (var change in changes)
        {
            foreach (var append in definition.Appends.Where(append => whenClauseEvaluator.Matches(append.When, change)))
            {
                var eventType = await ResolveEventType(append.EventType);
                if (eventType is null)
                {
                    continue;
                }

                events.Add(new EventToAppend(
                    EventSourceType.Default,
                    change.Key,
                    EventStreamType.All,
                    EventStreamId.Default,
                    eventType,
                    tags,
                    contentMapper.Map(append, change)));
            }
        }

        return events;
    }

    async Task<EventType?> ResolveEventType(string name)
    {
        var eventTypes = storage.GetEventStore(_eventStoreName).EventTypes;
        var eventTypeId = new EventTypeId(name);
        if (!await eventTypes.HasFor(eventTypeId))
        {
            logger.UnknownEventType(_captureId, name);
            return null;
        }

        var schema = await eventTypes.GetFor(eventTypeId);
        return schema.Type;
    }

    async Task Append(Capture capture, List<EventToAppend> events)
    {
        var eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(EventSequenceId.Log, _eventStoreName, EventStoreNamespaceName.Default));

        var result = await eventSequence.AppendMany(
            events,
            CorrelationId.New(),
            [CaptureCausation.For(capture)],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

        if (!result.IsSuccess)
        {
            logger.AppendingCapturedEventsFailed(
                capture.Name,
                _captureId,
                string.Join(", ", result.Errors.Select(error => error.ToString())));
        }
    }
}
